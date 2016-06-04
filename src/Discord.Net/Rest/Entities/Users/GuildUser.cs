﻿using Discord.API.Rest;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Model = Discord.API.GuildMember;

namespace Discord.Rest
{
    public class GuildUser : User, IGuildUser
    {
        private ImmutableArray<Role> _roles;

        public Guild Guild { get; }

        /// <inheritdoc />
        public bool IsDeaf { get; private set; }
        /// <inheritdoc />
        public bool IsMute { get; private set; }
        /// <inheritdoc />
        public DateTime JoinedAt { get; private set; }
        /// <inheritdoc />
        public string Nickname { get; private set; }

        /// <inheritdoc />
        public GuildPermissions GuildPermissions { get; private set; }

        /// <inheritdoc />
        public IReadOnlyList<Role> Roles => _roles;
        internal override DiscordClient Discord => Guild.Discord;

        internal GuildUser(Guild guild, Model model)
            : base(model.User)
        {
            Guild = guild;

            Update(model);
        }
        internal void Update(Model model)
        {
            IsDeaf = model.Deaf;
            IsMute = model.Mute;
            JoinedAt = model.JoinedAt.Value;
            Nickname = model.Nick;

            var roles = ImmutableArray.CreateBuilder<Role>(model.Roles.Length + 1);
            roles.Add(Guild.EveryoneRole);
            for (int i = 0; i < model.Roles.Length; i++)
                roles.Add(Guild.GetRole(model.Roles[i]));
            _roles = roles.ToImmutable();

            GuildPermissions = new GuildPermissions(Permissions.ResolveGuild(this));
        }

        public async Task Update()
        {
            var model = await Discord.ApiClient.GetGuildMember(Guild.Id, Id).ConfigureAwait(false);
            Update(model);
        }

        public async Task Kick()
        {
            await Discord.ApiClient.RemoveGuildMember(Guild.Id, Id).ConfigureAwait(false);
        }

        public ChannelPermissions GetPermissions(IGuildChannel channel)
        {
            if (channel == null) throw new ArgumentNullException(nameof(channel));
            return new ChannelPermissions(Permissions.ResolveChannel(this, channel, GuildPermissions.RawValue));
        }

        public async Task Modify(Action<ModifyGuildMemberParams> func)
        {
            if (func == null) throw new NullReferenceException(nameof(func));

            var args = new ModifyGuildMemberParams();
            func(args);

            bool isCurrentUser = (await Discord.GetCurrentUser().ConfigureAwait(false)).Id == Id;
            if (isCurrentUser && args.Nickname.IsSpecified)
            {
                var nickArgs = new ModifyCurrentUserNickParams { Nickname = args.Nickname.Value ?? "" };
                await Discord.ApiClient.ModifyCurrentUserNick(Guild.Id, nickArgs).ConfigureAwait(false);
                args.Nickname = new API.Optional<string>(); //Remove
            }

            if (!isCurrentUser || args.Deaf.IsSpecified || args.Mute.IsSpecified || args.Roles.IsSpecified)
            {
                await Discord.ApiClient.ModifyGuildMember(Guild.Id, Id, args).ConfigureAwait(false);
                if (args.Deaf.IsSpecified)
                    IsDeaf = args.Deaf.Value;
                if (args.Mute.IsSpecified)
                    IsMute = args.Mute.Value;
                if (args.Nickname.IsSpecified)
                    Nickname = args.Nickname.Value ?? "";
                if (args.Roles.IsSpecified)
                    _roles = args.Roles.Value.Select(x => Guild.GetRole(x)).Where(x => x != null).ToImmutableArray();
            }
        }


        IGuild IGuildUser.Guild => Guild;
        IReadOnlyList<IRole> IGuildUser.Roles => Roles;
        IVoiceChannel IGuildUser.VoiceChannel => null;

        GuildPermissions IGuildUser.GetGuildPermissions()
            => GuildPermissions;
        ChannelPermissions IGuildUser.GetPermissions(IGuildChannel channel)
            => GetPermissions(channel);
    }
}

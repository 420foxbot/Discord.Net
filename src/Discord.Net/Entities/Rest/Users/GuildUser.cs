﻿using Discord.API.Rest;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        public IReadOnlyList<Role> Roles => _roles;
        internal override DiscordRestClient Discord => Guild.Discord;

        internal GuildUser(Guild guild, Model model)
            : base(model.User)
        {
            Guild = guild;
        }
        internal void Update(Model model)
        {
            IsDeaf = model.Deaf;
            IsMute = model.Mute;
            JoinedAt = model.JoinedAt.Value;
            Nickname = model.Nick;

            var roles = ImmutableArray.CreateBuilder<Role>(model.Roles.Length + 1);
            roles[0] = Guild.EveryoneRole;
            for (int i = 0; i < model.Roles.Length; i++)
                roles[i + 1] = Guild.GetRole(model.Roles[i]);
            _roles = roles.ToImmutable();
        }

        public async Task Update()
        {
            var model = await Discord.BaseClient.GetGuildMember(Guild.Id, Id).ConfigureAwait(false);
            Update(model);
        }

        public bool HasRole(IRole role)
        {
            for (int i = 0; i < _roles.Length; i++)
            {
                if (_roles[i].Id == role.Id)
                    return true;
            }
            return false;
        }

        public async Task Kick()
        {
            await Discord.BaseClient.RemoveGuildMember(Guild.Id, Id).ConfigureAwait(false);
        }

        public GuildPermissions GetGuildPermissions()
        {
            return new GuildPermissions(PermissionHelper.Resolve(this));
        }
        public ChannelPermissions GetPermissions(IGuildChannel channel)
        {
            if (channel == null) throw new ArgumentNullException(nameof(channel));
            return new ChannelPermissions(PermissionHelper.Resolve(this, channel));
        }

        public async Task Modify(Action<ModifyGuildMemberParams> func)
        {
            if (func == null) throw new NullReferenceException(nameof(func));

            var args = new ModifyGuildMemberParams();
            func(args);
            var model = await Discord.BaseClient.ModifyGuildMember(Guild.Id, Id, args).ConfigureAwait(false);
            Update(model);
        }


        IGuild IGuildUser.Guild => Guild;
        IReadOnlyList<IRole> IGuildUser.Roles => Roles;
        ulong? IGuildUser.VoiceChannelId => null;

        ChannelPermissions IGuildUser.GetPermissions(IGuildChannel channel)
            => GetPermissions(channel);
    }
}

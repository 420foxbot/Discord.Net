﻿using Discord.API.Rest;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Model = Discord.API.Channel;

namespace Discord.WebSocket
{
    public abstract class GuildChannel : Channel, IGuildChannel
    {
        private ConcurrentDictionary<ulong, Overwrite> _overwrites;
        internal PermissionsCache _permissions;
        
        /// <summary> Gets the guild this channel is a member of. </summary>
        public Guild Guild { get; }

        /// <inheritdoc />
        public string Name { get; private set; }
        /// <inheritdoc />
        public int Position { get; private set; }
        public new abstract IEnumerable<GuildUser> Users { get; }
        
        /// <inheritdoc />
        public IReadOnlyDictionary<ulong, Overwrite> PermissionOverwrites => _overwrites;
        internal DiscordClient Discord => Guild.Discord;

        internal GuildChannel(Guild guild, Model model)
            : base(model.Id)
        {
            Guild = guild;

            Update(model);
        }
        internal virtual void Update(Model model)
        {
            Name = model.Name;
            Position = model.Position;

            var newOverwrites = new ConcurrentDictionary<ulong, Overwrite>();
            for (int i = 0; i < model.PermissionOverwrites.Length; i++)
            {
                var overwrite = model.PermissionOverwrites[i];
                newOverwrites[overwrite.TargetId] = new Overwrite(overwrite);
            }
            _overwrites = newOverwrites;
        }

        public async Task Modify(Action<ModifyGuildChannelParams> func)
        {
            if (func != null) throw new NullReferenceException(nameof(func));

            var args = new ModifyGuildChannelParams();
            func(args);
            await Discord.ApiClient.ModifyGuildChannel(Id, args).ConfigureAwait(false);
        }

        /// <summary> Gets a user in this channel with the given id. </summary>
        public new abstract GuildUser GetUser(ulong id);
        protected override User GetUserInternal(ulong id)
        {
            return GetUser(id).GlobalUser;
        }
        protected override IEnumerable<User> GetUsersInternal()
        {
            return Users.Select(x => x.GlobalUser);
        }

        /// <inheritdoc />
        public OverwritePermissions? GetPermissionOverwrite(IUser user)
        {
            Overwrite value;
            if (_overwrites.TryGetValue(Id, out value))
                return value.Permissions;
            return null;
        }
        /// <inheritdoc />
        public OverwritePermissions? GetPermissionOverwrite(IRole role)
        {
            Overwrite value;
            if (_overwrites.TryGetValue(Id, out value))
                return value.Permissions;
            return null;
        }
        /// <summary> Downloads a collection of all invites to this channel. </summary>
        public async Task<IEnumerable<InviteMetadata>> GetInvites()
        {
            var models = await Discord.ApiClient.GetChannelInvites(Id).ConfigureAwait(false);
            return models.Select(x => new InviteMetadata(Discord, x));
        }

        /// <inheritdoc />
        public async Task AddPermissionOverwrite(IUser user, OverwritePermissions perms)
        {
            var args = new ModifyChannelPermissionsParams { Allow = perms.AllowValue, Deny = perms.DenyValue };
            await Discord.ApiClient.ModifyChannelPermissions(Id, user.Id, args).ConfigureAwait(false);
        }
        /// <inheritdoc />
        public async Task AddPermissionOverwrite(IRole role, OverwritePermissions perms)
        {
            var args = new ModifyChannelPermissionsParams { Allow = perms.AllowValue, Deny = perms.DenyValue };
            await Discord.ApiClient.ModifyChannelPermissions(Id, role.Id, args).ConfigureAwait(false);
        }
        /// <inheritdoc />
        public async Task RemovePermissionOverwrite(IUser user)
        {
            await Discord.ApiClient.DeleteChannelPermission(Id, user.Id).ConfigureAwait(false);
        }
        /// <inheritdoc />
        public async Task RemovePermissionOverwrite(IRole role)
        {
            await Discord.ApiClient.DeleteChannelPermission(Id, role.Id).ConfigureAwait(false);
        }

        /// <summary> Creates a new invite to this channel. </summary>
        /// <param name="maxAge"> Time (in seconds) until the invite expires. Set to null to never expire. </param>
        /// <param name="maxUses"> The max amount  of times this invite may be used. Set to null to have unlimited uses. </param>
        /// <param name="isTemporary"> If true, a user accepting this invite will be kicked from the guild after closing their client. </param>
        /// <param name="withXkcd"> If true, creates a human-readable link. Not supported if maxAge is set to null. </param>
        public async Task<InviteMetadata> CreateInvite(int? maxAge = 1800, int? maxUses = null, bool isTemporary = false, bool withXkcd = false)
        {
            var args = new CreateChannelInviteParams
            {
                MaxAge = maxAge ?? 0,
                MaxUses = maxUses ?? 0,
                Temporary = isTemporary,
                XkcdPass = withXkcd
            };
            var model = await Discord.ApiClient.CreateChannelInvite(Id, args).ConfigureAwait(false);
            return new InviteMetadata(Discord, model);
        }

        /// <inheritdoc />
        public async Task Delete()
        {
            await Discord.ApiClient.DeleteChannel(Id).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public override string ToString() => Name;

        IGuild IGuildChannel.Guild => Guild;
        async Task<IInviteMetadata> IGuildChannel.CreateInvite(int? maxAge, int? maxUses, bool isTemporary, bool withXkcd)
            => await CreateInvite(maxAge, maxUses, isTemporary, withXkcd).ConfigureAwait(false);
        async Task<IEnumerable<IInviteMetadata>> IGuildChannel.GetInvites()
            => await GetInvites().ConfigureAwait(false);
        Task<IEnumerable<IGuildUser>> IGuildChannel.GetUsers()
            => Task.FromResult<IEnumerable<IGuildUser>>(Users);
        Task<IGuildUser> IGuildChannel.GetUser(ulong id)
            => Task.FromResult<IGuildUser>(GetUser(id));
        Task<IEnumerable<IUser>> IChannel.GetUsers()
            => Task.FromResult<IEnumerable<IUser>>(Users);
        Task<IEnumerable<IUser>> IChannel.GetUsers(int limit, int offset)
            => Task.FromResult<IEnumerable<IUser>>(Users.Skip(offset).Take(limit));
        Task<IUser> IChannel.GetUser(ulong id)
            => Task.FromResult<IUser>(GetUser(id));
        Task IUpdateable.Update() 
            => Task.CompletedTask;
    }
}

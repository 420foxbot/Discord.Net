﻿using Discord.API.Rest;
using Discord.Extensions;
using Discord.Logging;
using Discord.Net;
using Discord.Net.Queue;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Discord
{
    public class DiscordClient : IDiscordClient
    {
        public event Func<LogMessage, Task> Log;
        public event Func<Task> LoggedIn, LoggedOut;

        internal readonly Logger _discordLogger, _restLogger, _queueLogger;
        internal readonly SemaphoreSlim _connectionLock;
        internal readonly LogManager _log;
        internal readonly RequestQueue _requestQueue;
        internal bool _isDisposed;
        internal SelfUser _currentUser;

        public LoginState LoginState { get; private set; }
        public API.DiscordApiClient ApiClient { get; private set; }

        /// <summary> Creates a new REST-only discord client. </summary>
        public DiscordClient()
            : this(new DiscordConfig()) { }
        /// <summary> Creates a new REST-only discord client. </summary>
        public DiscordClient(DiscordConfig config)
        {
            _log = new LogManager(config.LogLevel);
            _log.Message += async msg => await Log.RaiseAsync(msg).ConfigureAwait(false);
            _discordLogger = _log.CreateLogger("Discord");
            _restLogger = _log.CreateLogger("Rest");
            _queueLogger = _log.CreateLogger("Queue");

            _connectionLock = new SemaphoreSlim(1, 1);

            _requestQueue = new RequestQueue();
            _requestQueue.RateLimitTriggered += async (id, bucket, millis) =>
            {
                await _queueLogger.WarningAsync($"Rate limit triggered (id = \"{id ?? "null"}\")").ConfigureAwait(false);
                if (bucket == null && id != null)
                    await _queueLogger.WarningAsync($"Unknown rate limit bucket \"{id ?? "null"}\"").ConfigureAwait(false);
            };
            
            ApiClient = new API.DiscordApiClient(config.RestClientProvider, (config as DiscordSocketConfig)?.WebSocketProvider, requestQueue: _requestQueue);
            ApiClient.SentRequest += async (method, endpoint, millis) => await _restLogger.VerboseAsync($"{method} {endpoint}: {millis} ms").ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task LoginAsync(TokenType tokenType, string token, bool validateToken = true)
        {
            await _connectionLock.WaitAsync().ConfigureAwait(false);
            try
            {
                await LoginInternalAsync(tokenType, token, validateToken).ConfigureAwait(false);
            }
            finally { _connectionLock.Release(); }
        }
        private async Task LoginInternalAsync(TokenType tokenType, string token, bool validateToken)
        {
            if (LoginState != LoginState.LoggedOut)
                await LogoutInternalAsync().ConfigureAwait(false);
            LoginState = LoginState.LoggingIn;

            try
            {
                await ApiClient.LoginAsync(tokenType, token).ConfigureAwait(false);

                if (validateToken)
                {
                    try
                    {
                        await ApiClient.ValidateTokenAsync().ConfigureAwait(false);
                    }
                    catch (HttpException ex)
                    {
                        throw new ArgumentException("Token validation failed", nameof(token), ex);
                    }
                }

                await OnLoginAsync().ConfigureAwait(false);

                LoginState = LoginState.LoggedIn;
            }
            catch (Exception)
            {
                await LogoutInternalAsync().ConfigureAwait(false);
                throw;
            }

            await LoggedIn.RaiseAsync().ConfigureAwait(false);
        }
        protected virtual Task OnLoginAsync() => Task.CompletedTask;

        /// <inheritdoc />
        public async Task LogoutAsync()
        {
            await _connectionLock.WaitAsync().ConfigureAwait(false);
            try
            {
                await LogoutInternalAsync().ConfigureAwait(false);
            }
            finally { _connectionLock.Release(); }
        }
        private async Task LogoutInternalAsync()
        {
            if (LoginState == LoginState.LoggedOut) return;
            LoginState = LoginState.LoggingOut;

            await ApiClient.LogoutAsync().ConfigureAwait(false);
            
            await OnLogoutAsync().ConfigureAwait(false);

            _currentUser = null;

            LoginState = LoginState.LoggedOut;

            await LoggedOut.RaiseAsync().ConfigureAwait(false);
        }
        protected virtual Task OnLogoutAsync() => Task.CompletedTask;

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<IConnection>> GetConnectionsAsync()
        {
            var models = await ApiClient.GetMyConnectionsAsync().ConfigureAwait(false);
            return models.Select(x => new Connection(x)).ToImmutableArray();
        }

        /// <inheritdoc />
        public virtual async Task<IChannel> GetChannelAsync(ulong id)
        {
            var model = await ApiClient.GetChannelAsync(id).ConfigureAwait(false);
            if (model != null)
            {
                if (model.GuildId.IsSpecified)
                {
                    var guildModel = await ApiClient.GetGuildAsync(model.GuildId.Value).ConfigureAwait(false);
                    if (guildModel != null)
                    {
                        var guild = new Guild(this, guildModel);
                        return guild.ToChannel(model);
                    }
                }
                else
                    return new DMChannel(this, new User(model.Recipient.Value), model);
            }
            return null;
        }
        /// <inheritdoc />
        public virtual async Task<IReadOnlyCollection<IDMChannel>> GetDMChannelsAsync()
        {
            var models = await ApiClient.GetMyDMsAsync().ConfigureAwait(false);
            return models.Select(x => new DMChannel(this, new User(x.Recipient.Value), x)).ToImmutableArray();
        }

        /// <inheritdoc />
        public virtual async Task<IInvite> GetInviteAsync(string inviteIdOrXkcd)
        {
            var model = await ApiClient.GetInviteAsync(inviteIdOrXkcd).ConfigureAwait(false);
            if (model != null)
                return new Invite(this, model);
            return null;
        }

        /// <inheritdoc />
        public virtual async Task<IGuild> GetGuildAsync(ulong id)
        {
            var model = await ApiClient.GetGuildAsync(id).ConfigureAwait(false);
            if (model != null)
                return new Guild(this, model);
            return null;
        }
        /// <inheritdoc />
        public virtual async Task<GuildEmbed?> GetGuildEmbedAsync(ulong id)
        {
            var model = await ApiClient.GetGuildEmbedAsync(id).ConfigureAwait(false);
            if (model != null)
                return new GuildEmbed(model);
            return null;
        }
        /// <inheritdoc />
        public virtual async Task<IReadOnlyCollection<IUserGuild>> GetGuildsAsync()
        {
            var models = await ApiClient.GetMyGuildsAsync().ConfigureAwait(false);
            return models.Select(x => new UserGuild(this, x)).ToImmutableArray();

        }
        /// <inheritdoc />
        public virtual async Task<IGuild> CreateGuildAsync(string name, IVoiceRegion region, Stream jpegIcon = null)
        {
            var args = new CreateGuildParams();
            var model = await ApiClient.CreateGuildAsync(args).ConfigureAwait(false);
            return new Guild(this, model);
        }

        /// <inheritdoc />
        public virtual async Task<IUser> GetUserAsync(ulong id)
        {
            var model = await ApiClient.GetUserAsync(id).ConfigureAwait(false);
            if (model != null)
                return new User(model);
            return null;
        }
        /// <inheritdoc />
        public virtual async Task<IUser> GetUserAsync(string username, string discriminator)
        {
            var model = await ApiClient.GetUserAsync(username, discriminator).ConfigureAwait(false);
            if (model != null)
                return new User(model);
            return null;
        }
        /// <inheritdoc />
        public virtual async Task<ISelfUser> GetCurrentUserAsync()
        {
            var user = _currentUser;
            if (user == null)
            {
                var model = await ApiClient.GetSelfAsync().ConfigureAwait(false);
                user = new SelfUser(this, model);
                _currentUser = user;
            }
            return user;
        }
        /// <inheritdoc />
        public virtual async Task<IReadOnlyCollection<IUser>> QueryUsersAsync(string query, int limit)
        {
            var models = await ApiClient.QueryUsersAsync(query, limit).ConfigureAwait(false);
            return models.Select(x => new User(x)).ToImmutableArray();
        }

        /// <inheritdoc />
        public virtual async Task<IReadOnlyCollection<IVoiceRegion>> GetVoiceRegionsAsync()
        {
            var models = await ApiClient.GetVoiceRegionsAsync().ConfigureAwait(false);
            return models.Select(x => new VoiceRegion(x)).ToImmutableArray();
        }
        /// <inheritdoc />
        public virtual async Task<IVoiceRegion> GetVoiceRegionAsync(string id)
        {
            var models = await ApiClient.GetVoiceRegionsAsync().ConfigureAwait(false);
            return models.Select(x => new VoiceRegion(x)).Where(x => x.Id == id).FirstOrDefault();
        }

        internal void Dispose(bool disposing)
        {
            if (!_isDisposed)
                _isDisposed = true;
        }
        /// <inheritdoc />
        public void Dispose() => Dispose(true);
        
        ConnectionState IDiscordClient.ConnectionState => ConnectionState.Disconnected;
        Task IDiscordClient.ConnectAsync() { throw new NotSupportedException(); }
        Task IDiscordClient.DisconnectAsync() { throw new NotSupportedException(); }
    }
}

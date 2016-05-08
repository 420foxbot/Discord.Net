﻿using Discord.API.Rest;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Model = Discord.API.Channel;

namespace Discord.Rest
{
    public class DMChannel : IDMChannel
    {
        /// <inheritdoc />
        public ulong Id { get; }
        internal DiscordRestClient Discord { get; }

        /// <inheritdoc />
        public DMUser Recipient { get; private set; }

        /// <inheritdoc />
        public DateTime CreatedAt => DateTimeHelper.FromSnowflake(Id);
        /// <inheritdoc />
        public IEnumerable<IUser> Users => ImmutableArray.Create<IUser>(Discord.CurrentUser, Recipient);

        internal DMChannel(DiscordRestClient discord, Model model)
        {
            Id = model.Id;
            Discord = discord;

            Update(model);
        }
        private void Update(Model model)
        {
            if (Recipient == null)
                Recipient = new DMUser(this, model.Recipient);
            else
                Recipient.Update(model.Recipient);
        }

        /// <inheritdoc />
        public IUser GetUser(ulong id)
        {
            if (id == Recipient.Id)
                return Recipient;
            else if (id == Discord.CurrentUser.Id)
                return Discord.CurrentUser;
            else
                return null;
        }
        public IEnumerable<IUser> GetUsers()
        {
            return ImmutableArray.Create<IUser>(Discord.CurrentUser, Recipient);
        }
        
        /// <inheritdoc />
        public async Task<IEnumerable<Message>> GetMessages(int limit = DiscordConfig.MaxMessagesPerBatch)
        {
            var args = new GetChannelMessagesParams { Limit = limit };
            var models = await Discord.BaseClient.GetChannelMessages(Id, args).ConfigureAwait(false);
            return models.Select(x => new Message(this, x));
        }
        /// <inheritdoc />
        public async Task<IEnumerable<Message>> GetMessages(ulong fromMessageId, Direction dir, int limit = DiscordConfig.MaxMessagesPerBatch)
        {
            var args = new GetChannelMessagesParams { Limit = limit };
            var models = await Discord.BaseClient.GetChannelMessages(Id, args).ConfigureAwait(false);
            return models.Select(x => new Message(this, x));
        }

        /// <inheritdoc />
        public async Task<Message> SendMessage(string text, bool isTTS = false)
        {
            var args = new CreateMessageParams { Content = text, IsTTS = isTTS };
            var model = await Discord.BaseClient.CreateMessage(Id, args).ConfigureAwait(false);
            return new Message(this, model);
        }
        /// <inheritdoc />
        public async Task<Message> SendFile(string filePath, string text = null, bool isTTS = false)
        {
            string filename = Path.GetFileName(filePath);
            using (var file = File.OpenRead(filePath))
            {
                var args = new UploadFileParams { Filename = filename, Content = text, IsTTS = isTTS };
                var model = await Discord.BaseClient.UploadFile(Id, file, args).ConfigureAwait(false);
                return new Message(this, model);
            }
        }
        /// <inheritdoc />
        public async Task<Message> SendFile(Stream stream, string filename, string text = null, bool isTTS = false)
        {
            var args = new UploadFileParams { Filename = filename, Content = text, IsTTS = isTTS };
            var model = await Discord.BaseClient.UploadFile(Id, stream, args).ConfigureAwait(false);
            return new Message(this, model);
        }

        /// <inheritdoc />
        public async Task DeleteMessages(IEnumerable<IMessage> messages)
        {
            await Discord.BaseClient.DeleteMessages(Id, new DeleteMessagesParam { MessageIds = messages.Select(x => x.Id) }).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task TriggerTyping()
        {
            await Discord.BaseClient.TriggerTypingIndicator(Id).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task Close()
        {
            await Discord.BaseClient.DeleteChannel(Id).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task Update()
        {
            var model = await Discord.BaseClient.GetChannel(Id).ConfigureAwait(false);
            Update(model);
        }

        /// <inheritdoc />
        public override string ToString() => $"@{Recipient} [DM]";
        
        IDMUser IDMChannel.Recipient => Recipient;

        Task<IEnumerable<IUser>> IChannel.GetUsers()
            => Task.FromResult(GetUsers());
        Task<IUser> IChannel.GetUser(ulong id)
            => Task.FromResult(GetUser(id));
        Task<IMessage> IMessageChannel.GetMessage(ulong id)
            => throw new NotSupportedException();
        async Task<IEnumerable<IMessage>> IMessageChannel.GetMessages(int limit)
            => await GetMessages(limit).ConfigureAwait(false);
        async Task<IEnumerable<IMessage>> IMessageChannel.GetMessages(ulong fromMessageId, Direction dir, int limit)
            => await GetMessages(fromMessageId, dir, limit).ConfigureAwait(false);
        async Task<IMessage> IMessageChannel.SendMessage(string text, bool isTTS)
            => await SendMessage(text, isTTS).ConfigureAwait(false);
        async Task<IMessage> IMessageChannel.SendFile(string filePath, string text, bool isTTS)
            => await SendFile(filePath, text, isTTS).ConfigureAwait(false);
        async Task<IMessage> IMessageChannel.SendFile(Stream stream, string filename, string text, bool isTTS)
            => await SendFile(stream, filename, text, isTTS).ConfigureAwait(false);
        async Task IMessageChannel.TriggerTyping()
            => await TriggerTyping().ConfigureAwait(false);
    }
}

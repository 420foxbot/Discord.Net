﻿using Discord.API.Rest;
using System;
using System.Threading.Tasks;
using Model = Discord.API.User;

namespace Discord.Rest
{
    public class SelfUser : User, ISelfUser
    {
        internal override DiscordRestClient Discord { get; }

        /// <inheritdoc />
        public string Email { get; private set; }
        /// <inheritdoc />
        public bool IsVerified { get; private set; }

        internal SelfUser(DiscordRestClient discord, Model model)
            : base(model)
        {
            Discord = discord;
        }
        internal override void Update(Model model)
        {
            base.Update(model);

            Email = model.Email;
            IsVerified = model.IsVerified;
        }

        /// <inheritdoc />
        public async Task Update()
        {
            var model = await Discord.BaseClient.GetCurrentUser().ConfigureAwait(false);
            Update(model);
        }

        /// <inheritdoc />
        public async Task Modify(Action<ModifyCurrentUserParams> func)
        {
            if (func != null) throw new NullReferenceException(nameof(func));

            var args = new ModifyCurrentUserParams();
            func(args);
            var model = await Discord.BaseClient.ModifyCurrentUser(args).ConfigureAwait(false);
            Update(model);
        }
    }
}

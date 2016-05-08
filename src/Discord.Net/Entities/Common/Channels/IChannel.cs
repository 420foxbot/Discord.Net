﻿using System.Collections.Generic;
using System.Threading.Tasks;

namespace Discord
{
    public interface IChannel : ISnowflakeEntity
    {
        /// <summary> Gets a collection of all users in this channel. </summary>
        Task<IEnumerable<IUser>> GetUsers();
        /// <summary> Gets a user in this channel with the provided id.</summary>
        Task<IUser> GetUser(ulong id);
    }
}

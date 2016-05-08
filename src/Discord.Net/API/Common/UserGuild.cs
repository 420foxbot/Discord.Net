﻿using Newtonsoft.Json;

namespace Discord.API
{
    public class UserGuild
    {
        [JsonProperty("id")]
        public ulong Id { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("icon")]
        public string Icon { get; set; }
        [JsonProperty("owner")]
        public bool Owner { get; set; }
        [JsonProperty("permissions")]
        public uint Permissions { get; set; }
    }
}

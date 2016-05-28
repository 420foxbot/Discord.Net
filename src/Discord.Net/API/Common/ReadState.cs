﻿using Newtonsoft.Json;

namespace Discord.API
{
    public class ReadState
    {
        [JsonProperty("id")]
        public ulong Id { get; set; }
        [JsonProperty("mention_count")]
        public int MentionCount { get; set; }
        [JsonProperty("last_message_id")]
        public ulong? LastMessageId { get; set; }
    }
}

﻿using System.Reflection;

namespace Discord
{
    public class DiscordConfig
    {
        public static string Version { get; } = typeof(DiscordConfig).GetTypeInfo().Assembly?.GetName().Version.ToString(3) ?? "Unknown";
        public static string UserAgent { get; } = $"DiscordBot (https://github.com/RogueException/Discord.Net, v{Version})";

        public const int GatewayAPIVersion = 3;

        public const string ClientAPIUrl = "https://discordapp.com/api/";
        public const string CDNUrl = "https://cdn.discordapp.com/";
        public const string InviteUrl = "https://discord.gg/";

        public const int MaxMessageSize = 2000;
        public const int MaxMessagesPerBatch = 100;
        public const int MaxUsersPerBatch = 1000;

        internal const int RestTimeout = 10000;
        internal const int MessageQueueInterval = 100;
        internal const int WebSocketQueueInterval = 100;

        /// <summary> Gets or sets the minimum log level severity that will be sent to the LogMessage event. </summary>
        public LogSeverity LogLevel { get; set; } = LogSeverity.Info;
    }
}

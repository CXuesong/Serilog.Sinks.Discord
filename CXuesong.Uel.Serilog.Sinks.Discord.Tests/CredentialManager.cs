using System;
using System.Collections.Generic;
using System.Text;

namespace CXuesong.Uel.Serilog.Sinks.Discord.Tests
{

    public static partial class CredentialManager
    {

        private static ulong discordWebhookId;
        private static string discordWebhookToken;

        static partial void Initialize();

        static CredentialManager()
        {
            Initialize();
        }

        public static DiscordWebhookMessenger CreateDiscordWebhookMessenger()
        {
            return new DiscordWebhookMessenger(discordWebhookId, discordWebhookToken);
        }

    }

}

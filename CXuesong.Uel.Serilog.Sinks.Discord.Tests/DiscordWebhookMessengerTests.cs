using System;
using System.Threading.Tasks;
using Xunit;

namespace CXuesong.Uel.Serilog.Sinks.Discord.Tests
{
    public class DiscordWebhookMessengerTests
    {

        [Fact]
        public void ArgumentTest()
        {
            Assert.Throws<ArgumentNullException>(() => new DiscordWebhookMessenger(0, null));
        }

        [Fact]
        public async Task PostMessageTest()
        {
            using (var messenger = CredentialManager.CreateDiscordWebhookMessenger())
            {
                messenger.PushMessage("Test message.");
                messenger.PushMessage("Test {0}. {1}", "message", 123);
                messenger.PushMessage((object)null);
                messenger.PushMessage("End of test.");
                await messenger.ShutdownAsync();
            }
        }

    }
}

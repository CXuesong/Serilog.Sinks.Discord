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
                messenger.PushMessage("PostMessageTest");
                messenger.PushMessage("Test message.");
                messenger.PushMessage((string)null);
                messenger.PushMessage("End of test.");
                await messenger.ShutdownAsync();
            }
        }

        [Fact]
        public async Task PostMessagePressureTest()
        {
            const int MESSAGE_COUNT = 50;
            using (var messenger = CredentialManager.CreateDiscordWebhookMessenger())
            {
                messenger.PushMessage("PostMessagePressureTest");
                for (int i = 1; i <= MESSAGE_COUNT; i++)
                {
                    messenger.PushMessage(string.Format("Test message {0}/{1}", i, MESSAGE_COUNT));
                    await Task.Delay(100);
                }
                messenger.PushMessage("End of test.");
                await messenger.ShutdownAsync();
            }
        }

    }
}

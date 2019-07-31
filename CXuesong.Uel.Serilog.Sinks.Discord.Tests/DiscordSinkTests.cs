using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Serilog;
using Xunit;

namespace CXuesong.Uel.Serilog.Sinks.Discord.Tests
{
    public class DiscordSinkTests
    {

        [Fact]
        public async Task TestLogging()
        {
            var messenger = CredentialManager.CreateDiscordWebhookMessenger();
            var logger = new LoggerConfiguration()
                .WriteTo.Discord(messenger, true)
                .CreateLogger();
            logger.Information("This is the information.");
        }

    }
}

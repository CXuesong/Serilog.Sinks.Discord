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
            using (var messenger = CredentialManager.CreateDiscordWebhookMessenger())
            {
                var logger = new LoggerConfiguration()
                    .MinimumLevel.Verbose()
                    .WriteTo.Discord(messenger, true)
                    .CreateLogger();
                logger.Verbose("This is the verbose log.");
                logger.Debug("This is the debug log.");
                logger.Information("This is the information.");
                logger.Warning("Heads up! This is a warning log.");
                logger.Error(new InvalidOperationException(), "Some error happens. {Text}", "Some text.");
                logger.Fatal("Something fatal from the logging.");
                await messenger.ShutdownAsync();
            }
        }

    }
}

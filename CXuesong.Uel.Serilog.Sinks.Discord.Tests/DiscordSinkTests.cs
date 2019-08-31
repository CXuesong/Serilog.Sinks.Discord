using System;
using System.Collections.Generic;
using System.Linq;
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
                    .AuditTo.Sink(new DiscordSink(messenger, null, true))
                    .CreateLogger();
                logger.Verbose("This is the verbose log.");
                logger.Debug("This is the debug log.");
                logger.Information("This is the information.");
                logger.Warning("Heads up! This is a warning log.");
                logger.Error(new InvalidOperationException(), "Some error happens. {Text}", "Some text.");
                logger.Fatal("Something fatal from the logging.");
                logger.Information(string.Join('\n', Enumerable.Range(1, 100).Select(i => i + " - This is long long information.")));
                await messenger.ShutdownAsync();
            }
        }

    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;

namespace CXuesong.Uel.Serilog.Sinks.Discord
{

    /// <summary>
    /// Serilog sink implementation for Discord Webhooks.
    /// </summary>
    public class DiscordSink : ILogEventSink, IDisposable
#if BCL_FEATURE_ASYNC_DISPOSABLE
, IAsyncDisposable
#endif
    {

        private readonly DiscordWebhookMessenger messenger;
        private readonly LogEventLevel minimumLevel;
        private readonly LoggingLevelSwitch? levelSwitch;
        private readonly IFormatProvider? formatProvider;
        private readonly bool disposeMessenger;

        public DiscordSink(DiscordWebhookMessenger messenger,
            LogEventLevel minimumLevel, LoggingLevelSwitch? levelSwitch,
            IFormatProvider? formatProvider, bool disposeMessenger)
        {
            this.messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            this.minimumLevel = minimumLevel;
            this.levelSwitch = levelSwitch;
            this.formatProvider = formatProvider;
            this.disposeMessenger = disposeMessenger;
        }

        /// <inheritdoc />
        public virtual void Emit(LogEvent logEvent)
        {
            if (logEvent.Level < minimumLevel) return;
            if (levelSwitch != null && logEvent.Level < levelSwitch.MinimumLevel) return;

            string? levelPrefix = null;
            var builder = new EmbedBuilder();
            switch (logEvent.Level)
            {
                case LogEventLevel.Verbose:
                    builder.WithColor(Color.DarkGrey);
                    levelPrefix = "[VER]";
                    break;
                case LogEventLevel.Debug:
                    levelPrefix = "[DEB]";
                    break;
                case LogEventLevel.Information:
                    builder.WithColor(Color.DarkBlue);
                    levelPrefix = "[INF]";
                    break;
                case LogEventLevel.Warning:
                    builder.WithColor(Color.DarkOrange);
                    levelPrefix = "[WRN]";
                    break;
                case LogEventLevel.Error:
                    levelPrefix = "[ERR]";
                    builder.WithColor(Color.DarkRed);
                    break;
                case LogEventLevel.Fatal:
                    levelPrefix = "[FAT]";
                    builder.WithColor(Color.Red);
                    break;
            }
            var message = logEvent.RenderMessage(formatProvider);
            string? restMessage = null;
            if (message.Length > 230)
            {
                var firstLineEnd = message.IndexOfAny(new[] { '\r', '\n' });
                if (firstLineEnd < 0 || firstLineEnd > 230)
                {
                    builder.WithTitle(message.Substring(0, 230) + "…");
                    restMessage = "…" + message.Substring(230);
                }
                else
                {
                    builder.WithTitle(message.Substring(0, firstLineEnd));
                    if (message[firstLineEnd] == '\r' && message.Length > firstLineEnd + 1 && message[firstLineEnd + 1] == '\n')
                    {
                        firstLineEnd++;
                    }
                    restMessage = message.Substring(firstLineEnd + 1);
                }
            }
            else
            {
                builder.WithTitle(message);
            }
            var sb = new StringBuilder();
            sb.Append('`');
            sb.Append(levelPrefix);
            sb.Append("` | `");
            sb.Append(logEvent.Timestamp.ToString("o", CultureInfo.InvariantCulture));
            sb.Append('`');
            if (logEvent.Exception != null)
            {
                sb.Append(" | `");
                sb.Append(logEvent.Exception.GetType());
                sb.Append('`');
                sb.AppendLine();
                var exMessage = logEvent.Exception.Message;
                sb.Append(exMessage.Length <= 1024 ? exMessage : exMessage.Substring(0, 1024));
            }
            if (restMessage != null)
            {
                if (sb.Length + restMessage.Length < 2030)
                {
                    sb.Insert(0, '\n');
                    sb.Insert(0, restMessage);
                }
                else
                {
                    var capacity = 2030 - sb.Length;
                    sb.Insert(0, '\n');
                    sb.Insert(0, "…");
                    sb.Insert(0, restMessage.Substring(0, capacity));
                }
            }
            builder.WithDescription(sb.ToString());
            messenger.PushMessage(builder.Build());
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (disposeMessenger)
                {
                    try
                    {
                        // Give underlying messenger some time to drain the queue.
                        messenger.ShutdownAsync().Wait(15 * 1000);
                    }
                    catch (Exception)
                    {
                        // According to an old convention, we shouldn't throw error in Dispose.
                    }
                    messenger.Dispose();
                }
            }
        }

#if BCL_FEATURE_ASYNC_DISPOSABLE
        /// <inheritdoc />
        public ValueTask DisposeAsync()
        {
            return messenger.DisposeAsync();
        }
#endif

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Provides extension methods for Discord logging sinks.
    /// </summary>
    public static class DiscordSinkExtensions
    {

        public static LoggerConfiguration Discord(this LoggerSinkConfiguration configuration,
            DiscordWebhookMessenger messenger,
            LogEventLevel minimumLevel = LogEventLevel.Verbose,
            LoggingLevelSwitch? levelSwitch = default,
            IFormatProvider? formatProvider = default,
            bool disposeMessenger = default)
        {
            return configuration.Sink(new DiscordSink(messenger, minimumLevel, levelSwitch, formatProvider, disposeMessenger));
        }

        public static LoggerConfiguration DiscordWebhook(this LoggerSinkConfiguration configuration,
            ulong webhookId, string webhookToken,
            LogEventLevel minimumLevel = LogEventLevel.Verbose,
            LoggingLevelSwitch? levelSwitch = default,
            IFormatProvider? formatProvider = default)
        {
            return configuration.Discord(new DiscordWebhookMessenger(webhookId, webhookToken),
                minimumLevel, levelSwitch, formatProvider, disposeMessenger: true);
        }

        public static LoggerConfiguration DiscordWebhook(this LoggerSinkConfiguration configuration,
            string webhookEndpointUrl,
            LogEventLevel minimumLevel = LogEventLevel.Verbose,
            LoggingLevelSwitch? levelSwitch = default,
            IFormatProvider? formatProvider = default)
        {
            return configuration.Discord(new DiscordWebhookMessenger(webhookEndpointUrl),
                minimumLevel, levelSwitch, formatProvider, disposeMessenger: true);
        }

    }

}

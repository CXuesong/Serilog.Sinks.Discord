using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Discord;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;

namespace CXuesong.Uel.Serilog.Sinks.Discord
{

    public class DiscordSink : ILogEventSink, IDisposable
    {

        private readonly DiscordWebhookMessenger messenger;
        private readonly IFormatProvider? formatProvider;
        private readonly bool disposeMessenger;

        public DiscordSink(DiscordWebhookMessenger messenger, IFormatProvider? formatProvider, bool disposeMessenger)
        {
            this.messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
            this.formatProvider = formatProvider;
            this.disposeMessenger = disposeMessenger;
        }

        /// <inheritdoc />
        public void Emit(LogEvent logEvent)
        {
            var message = logEvent.RenderMessage(formatProvider);
            string levelPrefix = null;
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
            builder.WithTitle(message);
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
                sb.Append(logEvent.Exception.Message);
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

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

    }

    public static class DiscordSinkExtensions
    {

        public static LoggerConfiguration Discord(this LoggerSinkConfiguration configuration,
            DiscordWebhookMessenger messenger, IFormatProvider? formatProvider, bool disposeMessenger)
        {
            return configuration.Sink(new DiscordSink(messenger, formatProvider, disposeMessenger));
        }

        public static LoggerConfiguration Discord(this LoggerSinkConfiguration configuration,
            DiscordWebhookMessenger messenger, IFormatProvider? formatProvider)
        {
            return configuration.Sink(new DiscordSink(messenger, formatProvider, false));
        }

        public static LoggerConfiguration Discord(this LoggerSinkConfiguration configuration,
            DiscordWebhookMessenger messenger, bool disposeMessenger)
        {
            return configuration.Sink(new DiscordSink(messenger, null, disposeMessenger));
        }

        public static LoggerConfiguration Discord(this LoggerSinkConfiguration configuration,
            DiscordWebhookMessenger messenger)
        {
            return configuration.Sink(new DiscordSink(messenger, null, false));
        }

        public static LoggerConfiguration DiscordWebhook(this LoggerSinkConfiguration configuration,
            ulong webhookId, string webhookToken, IFormatProvider? formatProvider)
        {
            return configuration.Sink(new DiscordSink(new DiscordWebhookMessenger(webhookId, webhookToken),
                formatProvider, true));
        }

        public static LoggerConfiguration DiscordWebhook(this LoggerSinkConfiguration configuration,
            ulong webhookId, string webhookToken)
        {
            return configuration.Sink(new DiscordSink(new DiscordWebhookMessenger(webhookId, webhookToken),
                null, true));
        }

    }

}

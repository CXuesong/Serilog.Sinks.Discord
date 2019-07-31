using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord.Net.Rest;
using Discord.Rest;
using Discord.Webhook;

namespace CXuesong.Uel.Serilog.Sinks.Discord
{

    /// <summary>
    /// Queuing and sending textual message with Discord Webhook.
    /// </summary>
    public class DiscordWebhookMessenger : IDisposable
    {

        private readonly ulong webhookId;
        private readonly Task workerTask;
        private readonly BlockingCollection<string?> impendingMessages = new BlockingCollection<string?>(1024);
        private readonly SemaphoreSlim impendingMessagesSemaphore = new SemaphoreSlim(0, 1024);
        private readonly CancellationTokenSource shutdownCts = new CancellationTokenSource();

        /// <param name="id">Discord webhook ID.</param>
        /// <param name="token">Discord webhook token.</param>
        public DiscordWebhookMessenger(ulong id, string token)
        {
            this.webhookId = id;
            workerTask = WorkerAsync(token, shutdownCts.Token);
        }

        /// <inheritdoc cref="PushMessage(string?)"/>
        public void PushMessage(string format, object? arg0)
        {
            PushMessage(string.Format(format, arg0));
        }

        /// <inheritdoc cref="PushMessage(string?)"/>
        public void PushMessage(string format, object? arg0, object? arg1)
        {
            PushMessage(string.Format(format, arg0, arg1));
        }

        /// <inheritdoc cref="PushMessage(string?)"/>
        public void PushMessage(string format, params object?[] args)
        {
            PushMessage(string.Format(format, args));
        }

        /// <inheritdoc cref="PushMessage(string?)"/>
        public void PushMessage(IFormatProvider formatProvider, string format, object? arg0)
        {
            PushMessage(string.Format(formatProvider, format, arg0));
        }

        /// <inheritdoc cref="PushMessage(string?)"/>
        public void PushMessage(IFormatProvider formatProvider, string format, object? arg0, object? arg1)
        {
            PushMessage(string.Format(formatProvider, format, arg0, arg1));
        }

        /// <inheritdoc cref="PushMessage(string?)"/>
        public void PushMessage(IFormatProvider formatProvider, string format, params object?[] args)
        {
            PushMessage(string.Format(formatProvider, format, args));
        }

        /// <inheritdoc cref="PushMessage(string?)"/>
        public void PushMessage(object? value)
        {
            PushMessage(value?.ToString());
        }

        /// <summary>
        /// Pushes a new message into the message queue.
        /// </summary>
        /// <param name="message">Content of the message.</param>
        /// <exception cref="InvalidOperationException"><see cref="ShutdownAsync"/> has been called.</exception>
        /// <exception cref="Exception">Any exception from the message dispatching worker will be propagated from this method.</exception>
        public void PushMessage(string? message)
        {
            if (shutdownCts.IsCancellationRequested) 
                throw new InvalidOperationException("The messenger is shutting down.");

            // Propagate worker's exceptions, if any.
            if (workerTask.IsFaulted) workerTask.GetAwaiter().GetResult();

            lock (impendingMessages)
                impendingMessages.Add(message);
            try
            {
                impendingMessagesSemaphore.Release();
            }
            catch (ObjectDisposedException)
            {
                // In case impendingMessagesSemaphore has been released.
            }
        }

        // Long-running worker thread.
        private async Task WorkerAsync(string token, CancellationToken ct)
        {
            using (impendingMessagesSemaphore)
            using (var client = new DiscordWebhookClient(webhookId, token,
                new DiscordRestConfig { RestClientProvider = DefaultRestClientProvider.Create(true) }))
            {
                try
                {
                    while (!ct.IsCancellationRequested)
                    {
                        await impendingMessagesSemaphore.WaitAsync(ct);
                        var message = impendingMessages.Take();
                        await client.SendMessageAsync(message);
                    }
                }
                catch (OperationCanceledException)
                {
                    // cancelled from WaitAsync
                }
                // Cleanup
                while (impendingMessages.TryTake(out var message))
                {
                    await client.SendMessageAsync(message);
                }
            }
        }

        /// <summary>
        /// Wait until the queued messages has been drained, and shutdown the worker task.
        /// </summary>
        /// <returns>The task that completes when the worker has ended, and throws if there is error in the worker task.</returns>
        public Task ShutdownAsync()
        {
            shutdownCts.Cancel();
            return workerTask;
        }

        /// <inheritdoc />
        /// <remarks>You need to call <see cref="ShutdownAsync"/> before disposing the instance, to ensure all the logs has been reliably sent to the server.</remarks>
        public void Dispose()
        {
            shutdownCts.Cancel();
            workerTask.Dispose();
            impendingMessages.Dispose();
            impendingMessagesSemaphore.Dispose();
            shutdownCts.Dispose();
        }

    }
}

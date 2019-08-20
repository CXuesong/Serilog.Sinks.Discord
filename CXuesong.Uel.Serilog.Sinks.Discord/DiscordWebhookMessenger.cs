using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
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
        private readonly ConcurrentQueue<Embed> impendingMessages = new ConcurrentQueue<Embed>();
        private readonly SemaphoreSlim impendingMessagesSemaphore = new SemaphoreSlim(0);
        private readonly CancellationTokenSource shutdownCts = new CancellationTokenSource();
        private TimeSpan _RequestThrottleTime = TimeSpan.FromSeconds(0);
        private int _MaxMessagesPerPack = 100;

        /// <param name="id">Discord webhook ID.</param>
        /// <param name="token">Discord webhook token.</param>
        /// <exception cref="ArgumentNullException"><paramref name="token"/> is <c>null</c>.</exception>
        public DiscordWebhookMessenger(ulong id, string token)
        {
            if (token == null) throw new ArgumentNullException(nameof(token));
            webhookId = id;
            workerTask = WorkerAsync(token, shutdownCts.Token);
        }

        public int MaxMessagesPerPack
        {
            get { return _MaxMessagesPerPack; }
            set
            {
                if (_MaxMessagesPerPack < 1)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Value should not be less than 1.");
                _MaxMessagesPerPack = value;
            }
        }

        public TimeSpan RequestThrottleTime
        {
            get { return _RequestThrottleTime; }
            set
            {
                if (_RequestThrottleTime <= TimeSpan.Zero)
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Value should be non-negative.");
                _RequestThrottleTime = value;

            }
        }

        /// <inheritdoc cref="PushMessage(string?)"/>
        public void PushMessage(Embed embed)
        {
            if (embed == null) throw new ArgumentNullException(nameof(embed));
            // Propagate worker's exceptions, if any.
            if (workerTask.IsFaulted) workerTask.GetAwaiter().GetResult();

            impendingMessages.Enqueue(embed);
            try
            {
                impendingMessagesSemaphore.Release();
            }
            catch (ObjectDisposedException)
            {
                // In case impendingMessagesSemaphore has been disposed.
            }
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
            var embed = new EmbedBuilder().WithTitle("").WithDescription(message ?? "").Build();
            PushMessage(embed);
        }

        // Long-running worker thread.
        private async Task WorkerAsync(string token, CancellationToken ct)
        {
            var config = new DiscordRestConfig { RestClientProvider = DefaultRestClientProvider.Create(true) };
#if DEBUG
            config.LogLevel = LogSeverity.Info;
#endif
            using (var client = new DiscordWebhookClient(webhookId, token, config))
            {
#if DEBUG
                client.Log += log =>
                {
                    Debug.WriteLine(log.ToString());
                    return Task.CompletedTask;
                };
#endif
                var messageBuffer = new List<Embed>();
                do
                {
                    try
                    {
                        await Task.Delay(_RequestThrottleTime, ct);
                    }
                    catch (OperationCanceledException)
                    {
                        // cancelled from Delay
                    }
                    try
                    {
                        // Take 1
                        // Consider the case where ct is cancelled and we are draining the queue.
                        if (!impendingMessagesSemaphore.Wait(0))
                        {
                            await impendingMessagesSemaphore.WaitAsync(ct);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // cancelled from WaitAsync
                    }
                    for (int i = 0; i < _MaxMessagesPerPack; i++)
                    {
                        // Consume 1
                        var result = impendingMessages.TryDequeue(out var embed);
                        Debug.Assert(result);
                        if (result)
                            messageBuffer.Add(embed);
                        // Take another
                        if (!impendingMessagesSemaphore.Wait(0))
                            break;
                    }
                    try
                    {
                        await client.SendMessageAsync(embeds: messageBuffer);
                    }
                    catch (Exception e)
                    {
                        throw;
                    }
                    messageBuffer.Clear();
                } while (!ct.IsCancellationRequested || impendingMessagesSemaphore.CurrentCount > 0);
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
            if (workerTask.IsCompleted)
                // or we will get InvalidOperationException.
                workerTask.Dispose();
            impendingMessagesSemaphore.Dispose();
            shutdownCts.Dispose();
        }

    }
}

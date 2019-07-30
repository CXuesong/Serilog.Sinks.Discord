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
    public class DiscordWebhookMessenger : IDisposable
    {

        private readonly ulong webhookId;
        private readonly Task workerTask;
        private readonly BlockingCollection<string> impendingMessages = new BlockingCollection<string>(1024);
        private readonly SemaphoreSlim impendingMessagesSemaphore = new SemaphoreSlim(0, 1024);
        private readonly CancellationTokenSource disposalCts = new CancellationTokenSource();

        public DiscordWebhookMessenger(ulong id, string token)
        {
            this.webhookId = id;
            workerTask = WorkerAsync(token, disposalCts.Token);
        }

        public void PushMessage(string format, object arg0)
        {
            PushMessage(string.Format(format, arg0));
        }

        public void PushMessage(string format, object arg0, object arg1)
        {
            PushMessage(string.Format(format, arg0, arg1));
        }

        public void PushMessage(string format, params object[] args)
        {
            PushMessage(string.Format(format, args));
        }

        public void PushMessage(string message)
        {
            if (disposalCts.IsCancellationRequested) return;

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

        /// <inheritdoc />
        public void Dispose()
        {
            disposalCts.Cancel();
            try
            {
                workerTask.Wait(15 * 1000);
            }
            catch (Exception)
            {
                // According to an old convention, we shouldn't throw error in Dispose.
            }
            workerTask.Dispose();
            impendingMessages.Dispose();
            impendingMessagesSemaphore.Dispose();
            disposalCts.Dispose();
        }

    }
}

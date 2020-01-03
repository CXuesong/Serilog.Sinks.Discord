[CXuesong.Uel.Serilog.Sinks.Discord](https://www.nuget.org/packages/CXuesong.Uel.Serilog.Sinks.Discord) | ![NuGet version (CXuesong.Uel.Serilog.Sinks.Discord)](https://img.shields.io/nuget/vpre/CXuesong.Uel.Serilog.Sinks.Discord.svg?style=flat-square)

# Serilog.Sinks.Discord

A *Utility Extension Library* that provides sink for [Serilog](https://github.com/serilog/serilog) to [Discord Webhook](https://support.discordapp.com/hc/en-us/articles/228383668-Intro-to-Webhooks). Currently it's Webhook-based. In the future there might be some more functionalities. If you would like to see some improvements be done soon, please open an issue in this repository.

The package is now available on NuGet. You may install the main package using the following command

```powershell
#  Package Management Console
Install-Package CXuesong.Uel.Serilog.Sinks.Discord
#  .NET CLI
dotnet add package CXuesong.Uel.Serilog.Sinks.Discord
```

## Usage

```c#
using Serilog;
using CXuesong.Uel.Serilog.Sinks.Discord;

var logger = new LoggerConfiguration()
    .WriteTo.Discord(YOUR_WEBHOOK_ID, "YOUR_WEBHOOK_TOKEN")
    .CreateLogger();
logger.Information("This is the information.");
logger.Dispose();

// -- or --

var messenger = new DiscordWebhookMessenger();
var logger = new LoggerConfiguration()
    .WriteTo.Discord(messenger)
    .CreateLogger();
logger.Information("This is the information.");
await messenger.ShutdownAsync();
messenger.Dispose();
logger.Dispose();
```

## Remarks

The logs are queued inside the sink (or `DiscordWebhookMessenger`, to be more exact). To ensure all the logs has been sent successfully to the Discord server, await `ShutdownAsync`. This means you will need to construct your own `DiscordWebhookMessenger` before calling `.WriteTo.Discord`. 

`DiscordSink.Dispose` will automatically call `ShutdownAsync` and wait for at most 15 secs. in its `Dispose` implementation, when `disposeMessenger` is `true` (by default it's `false`) and the underlying `DiscordWebhookMessenger`  has not been called `ShutdownAsync` on.

## Appendix: How to retrieve Webhook ID and Token

Follow the guidance of this article: [Intro to Webhooks](https://support.discordapp.com/hc/en-us/articles/228383668-Intro-to-Webhooks). On the same dialog where you create the Webhook, you should be able to see the Webhook URL like this

```
https://discordapp.com/api/webhooks/[YOUR_WEBHOOK_ID]/[YOUR_WEBHOOK_TOKEN]
```

Match your Webhook URL with the pattern above. Now you have all the information.
using System;
using Telega.Client;
using Telega.Playground;
using Telega.Playground.Snippets;

Console.WriteLine("Connecting to Telegram.");
using var tg = await TelegramClient.Connect(Authorizer.ApiId);
await Authorizer.Authorize(tg);

await SendMultiMedia.Run(tg);
await ListenToUpdates.Run(tg);

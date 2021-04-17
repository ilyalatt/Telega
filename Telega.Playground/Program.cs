using System;
using Telega.Client;
using Telega.Playground;
using Telega.Playground.Snippets;

Console.WriteLine("Connecting to Telegram.");
using var tg = new TelegramClient();
await Authorizer.Authorize(tg);

await PrintUserInfo.Run(tg);
// await SendMultiMedia.Run(tg);
await ListenToUpdates.Run(tg);

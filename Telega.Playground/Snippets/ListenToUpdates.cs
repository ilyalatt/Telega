using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using NullExtensions;
using Telega.Client;
using Telega.Rpc.Dto.Functions;

namespace Telega.Playground.Snippets {
    static class ListenToUpdates {
        // pings each 10 seconds and ignores exceptions
        // tg.Call tries to reconnect if the connection is broken
        // so this construction should keep TelegramClient alive
        static IDisposable KeepAlive(TelegramClient tg) => Observable
            .Timer(dueTime: TimeSpan.Zero, period: TimeSpan.FromSeconds(10))
            .Select(_ => Observable.FromAsync(() => tg.Call(new Ping(pingId: 0))).Materialize())
            .Concat()
            .Subscribe();
        
        public static async Task Run(TelegramClient tg) {
            Console.WriteLine("Listening to updates until exit");
            using var _keepAliveSub = KeepAlive(tg);
            using var _updateSub = tg.Updates.Stream.Subscribe(
                onNext: updatesType => {
                    var messageText = updatesType.Match(
                        updateShortMessageTag: x => "updateShortMessageTag: " + x.Message,
                        updateShortChatMessageTag: x => "updateShortChatMessageTag: " + x.Message,
                        updateShortTag: update => update.Update.Match(
                            newMessageTag: msg => msg.Message.Default.NSelect(x => "newMessageTag: " + x.Message),
                            editMessageTag: msg => msg.Message.Default.NSelect(x => "editMessageTag: " + x.Message),
                            editChannelMessageTag: msg =>
                                msg.Message.Default.NSelect(x => "editChannelMessageTag: " + x.Message),
                            _: () => null
                        ),
                        _: () => null
                    );
                    messageText.NForEach(Console.WriteLine);
                },
                onError: Console.WriteLine
            );
            await Task.Delay(Timeout.Infinite);
        }
    }
}
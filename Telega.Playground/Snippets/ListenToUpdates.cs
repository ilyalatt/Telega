using System;
using System.Threading;
using System.Threading.Tasks;
using Telega.Client;
using Telega.Utils;

namespace Telega.Playground.Snippets {
    static class ListenToUpdates {
        public static async Task Run(TelegramClient tg) {
            Console.WriteLine("Listening to updates until exit.");
            tg.Updates.Stream.Subscribe(
                onNext: updatesType => {
                    var messageText = updatesType.Match(
                        updateShortMessageTag: x => "updateShortMessageTag: " + x.Message,
                        updateShortChatMessageTag: x => "updateShortChatMessageTag: " + x.Message,
                        updateShortTag: update => update.Update.Match(
                            newMessageTag: msg => msg.Message.Default.NMap(x => "newMessageTag: " + x.Message),
                            editMessageTag: msg => msg.Message.Default.NMap(x => "editMessageTag: " + x.Message),
                            editChannelMessageTag: msg =>
                                msg.Message.Default.NMap(x => "editChannelMessageTag: " + x.Message),
                            _: () => null
                        ),
                        _: () => null
                    );
                    messageText.NIter(Console.WriteLine);
                },
                onError: Console.WriteLine
            );

            tg.Updates.Stream.Subscribe(
                onNext: updatesType => {
                    var messageText = updatesType.Match(
                        updateShortMessageTag: x => "updateShortMessageTag: " + x.Message,
                        updateShortChatMessageTag: x => "updateShortChatMessageTag: " + x.Message,
                        updateShortTag: update => update.Update.Match(
                            newMessageTag: msg => msg.Message.Default.NMap(x => "newMessageTag: " + x.Message),
                            editMessageTag: msg => msg.Message.Default.NMap(x => "editMessageTag: " + x.Message),
                            editChannelMessageTag: msg =>
                                msg.Message.Default.NMap(x => "editChannelMessageTag: " + x.Message),
                            _: () => null
                        ),
                        _: () => null
                    );
                    messageText.NIter(Console.WriteLine);
                },
                onError: Console.WriteLine
            );

            await Task.Delay(Timeout.Infinite);
        }
    }
}
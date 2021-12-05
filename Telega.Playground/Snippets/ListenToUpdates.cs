using System;
using System.Threading;
using System.Threading.Tasks;
using NullExtensions;
using Telega.Client;
using Telega.Rpc.Dto.Functions;

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
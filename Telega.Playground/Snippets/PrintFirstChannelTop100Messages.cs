using System;
using System.Linq;
using System.Threading.Tasks;
using NullExtensions;
using Telega.Client;
using Telega.Rpc.Dto.Types;

namespace Telega.Playground.Snippets {
    static class PrintFirstChannelTop100Messages {
        public static async Task Run(TelegramClient tg) {
            var dialogs = await tg.Messages.GetDialogs();
            var firstChannel = dialogs
                .Default!
                .Chats
                .NChoose(x => x.Channel)
                .FirstOrDefault() ?? throw new Exception("A channel is not found.");
            var channelPeer = new InputPeer.ChannelTag(
                channelId: firstChannel.Id,
                accessHash: firstChannel.AccessHash!.Value
            );
            
            var top100Messages = await tg.Messages.GetHistory(channelPeer, limit: 100);
            Console.WriteLine("Here are top 100 messages from the first channel:");
            top100Messages.Channel!.Messages.NForEach(msg => {
                Console.WriteLine(msg);
                Console.WriteLine();
            });
        }
    }
}
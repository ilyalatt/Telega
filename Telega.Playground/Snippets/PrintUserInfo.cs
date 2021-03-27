using System;
using System.Threading.Tasks;
using Telega.Client;
using Telega.Rpc.Dto.Functions.Users;
using Telega.Rpc.Dto.Types;

namespace Telega.Playground.Snippets {
    static class PrintUserInfo {
        public static async Task Run(TelegramClient tg) {
            var myInfo = await tg.Call(new GetFullUser(new InputUser.SelfTag()));
            Console.WriteLine("Here is info about your account.");
            Console.WriteLine(myInfo);
        }
    }
}
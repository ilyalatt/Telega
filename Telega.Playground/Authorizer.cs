using System;
using System.Security;
using System.Threading.Tasks;
using Telega.Client;
using Telega.Utils;

namespace Telega.Playground {
    public static class Authorizer {
        static T ReadString<T>(
            Func<string, T?> mapper
        ) {
            while (true) {
                var input = mapper(Console.ReadLine() ?? "");
                if (input != null) {
                    return input;
                }
                Console.WriteLine("Invalid input. Try again.");
            }
        }
        
        static SecureString ReadPassword() {
            var pass = new SecureString();
            while (true) {
                var input = Console.ReadKey(true);
                if (input.Key == ConsoleKey.Enter) {
                    Console.WriteLine();
                    break;
                }

                if (!char.IsControl(input.KeyChar)) {
                    pass.AppendChar(input.KeyChar);
                    Console.Write("*");
                }
                else if (input.Key == ConsoleKey.Backspace && pass.Length > 0) {
                    pass.RemoveAt(pass.Length - 1);
                    Console.Write("\b \b");
                }
            }
            return pass;
        }

        static async Task SignInViaCode(TelegramClient tg) {
            Console.WriteLine("Type your phone number.");
            var phone = ReadString(x => x
               .Replace(" ", "")
               .Replace("(", "")
               .Replace(")", "")
               .Trim()
               .Apply(x => x.Length > 0 ? x : null)
            );
            
            Console.WriteLine("Requesting login code.");
            var codeHash = await tg.Auth.SendCode(phone);

            while (true) {
                try {
                    Console.WriteLine("Type the login code.");
                    var code = ReadString(x => x
                       .Apply(x => int.TryParse(x, out var res) ? (int?) res : null)
                       .Apply(x => x?.ToString())
                    );
                    await tg.Auth.SignIn(phone, codeHash, code);
                    break;
                }
                catch (TgInvalidPhoneCodeException) {
                    Console.WriteLine("Invalid login code. Try again.");
                }
            }
        }

        static async Task SignInViaPassword(TelegramClient tg) {
            while (true) {
                Console.WriteLine("Type the password.");
                var password = ReadPassword();
                try {
                    await tg.Auth.CheckPassword(password);
                    break;
                }
                catch (TgInvalidPasswordException) {
                    Console.WriteLine("Invalid password. Try again.");
                }
            }
        }

        public static async Task Authorize(TelegramClient tg) {
            if (await tg.Auth.IsAuthorized()) {
                Console.WriteLine("You're already authorized.");
                return;
            }

            try {
                Console.WriteLine("Authorizing.");
                await SignInViaCode(tg);
            }
            catch (TgPasswordNeededException) {
                Console.WriteLine("Cloud password is needed.");
                await SignInViaPassword(tg);
            }

            Console.WriteLine("Authorization is completed.");
        }
    }
}
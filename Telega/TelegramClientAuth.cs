using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using LanguageExt;
using Telega.Rpc.Dto;
using Telega.Rpc.Dto.Functions.Account;
using Telega.Rpc.Dto.Functions.Auth;
using Telega.Rpc.Dto.Types;
using Telega.Rpc.Dto.Types.Account;
using static LanguageExt.Prelude;

namespace Telega
{
    public sealed class TelegramClientAuth
    {
        readonly TelegramClient _tg;
        internal TelegramClientAuth(Some<TelegramClient> tg) => _tg = tg;


        public async Task<string> SendCode(Some<string> phoneNumber)
        {
            var resp = await _tg.Call(new SendCode(
                phoneNumber: phoneNumber,
                apiId: _tg._apiId,
                apiHash: _tg._apiHash,
                allowFlashcall: false,
                currentNumber: None
            ));
            return resp.PhoneCodeHash;
        }

        public async Task<User> SignIn(Some<string> phoneNumber, Some<string> phoneCodeHash, Some<string> code)
        {
            var resp = await _tg.Call(new SignIn(phoneNumber: phoneNumber, phoneCodeHash: phoneCodeHash, phoneCode: code));
            var user = resp.User;
            await _tg.SetAuthorized(user);
            return user;
        }

        public async Task<Password> GetPasswordInfo() =>
            await _tg.Call(new GetPassword());

        public async Task<User> CheckPassword(Some<Password.Tag> password, Some<string> passwordStr)
        {
            var passwordBytes = Encoding.UTF8.GetBytes(passwordStr.Value);
            var currentSalt = password.Value.CurrentSalt.ToArrayUnsafe();
            // TODO: check new salt
            var rv = currentSalt.Concat(passwordBytes).Concat(currentSalt);

            var hash = new SHA256Managed();
            var passwordHash = hash.ComputeHash(rv.ToArray());

            var request = new CheckPassword(passwordHash: passwordHash.ToBytesUnsafe());

            var res = await _tg.Call(request);
            var user = res.User;

            await _tg.SetAuthorized(user);

            return user;
        }

        public async Task<User> SignUp(
            Some<string> phoneNumber,
            Some<string> phoneCodeHash,
            Some<string> code,
            Some<string> firstName,
            Some<string> lastName
        ) {
            var res = await _tg.Call(new SignUp(
                phoneNumber: phoneNumber,
                phoneCode: code,
                phoneCodeHash: phoneCodeHash,
                firstName: firstName,
                lastName: lastName
            ));
            var user = res.User;
            await _tg.SetAuthorized(user);
            return user;
        }
    }
}

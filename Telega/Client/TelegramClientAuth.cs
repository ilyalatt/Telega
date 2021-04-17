using System;
using System.Security;
using System.Threading.Tasks;
using Telega.Connect;
using Telega.Rpc.Dto.Functions.Account;
using Telega.Rpc.Dto.Functions.Auth;
using Telega.Rpc.Dto.Functions.Users;
using Telega.Rpc.Dto.Types;
using Telega.Rpc.Dto.Types.Account;

namespace Telega.Client {
    public sealed class TelegramClientAuth {
        readonly TgBellhop _tg;

        internal TelegramClientAuth(
            TgBellhop tg
        ) {
            _tg = tg;
        }

        public async Task<bool> IsAuthorized() {
            if (_tg.Session?.RpcState == null) {
                return false;
            }

            try {
                await _tg.Call(new GetFullUser(new InputUser.SelfTag())).ConfigureAwait(false);
                return true;
            }
            catch (TgNotAuthenticatedException) {
                return false;
            }
        }

        public async Task<string> SendCode(string phoneNumber) {
            var (id, hash) = _tg.Session?.RpcConfig?.Credentials!;
            var res = await _tg.Call(new SendCode(
                phoneNumber: phoneNumber,
                apiId: id,
                apiHash: hash,
                new CodeSettings(
                    allowFlashcall: false,
                    currentNumber: false,
                    allowAppHash: false
                )
            )).ConfigureAwait(false);
            return res.PhoneCodeHash;
        }

        public async Task<User.DefaultTag> SignIn(string phoneNumber, string phoneCodeHash, string code) {
            var res = await _tg.Call(new SignIn(
                phoneNumber: phoneNumber,
                phoneCodeHash: phoneCodeHash,
                phoneCode: code
            )).ConfigureAwait(false);
            return res.Default!.User.Default!;
        }

        public async Task<Password> GetPasswordInfo() =>
            await _tg.Call(new GetPassword()).ConfigureAwait(false);

        public async Task<User.DefaultTag> CheckPassword(SecureString password) {
            var passwordInfo = await GetPasswordInfo().ConfigureAwait(false);
            if (!passwordInfo.HasPassword) {
                throw new ArgumentException("the account does not have a password", nameof(passwordInfo));
            }

            var currentAlgo = passwordInfo.CurrentAlgo
                ?? throw new ArgumentException("there is no CurrentAlgo", nameof(passwordInfo));
            var algo = currentAlgo
               .Sha256Sha256Pbkdf2Hmacsha512Iter100000Sha256ModPow
               ?? throw new ArgumentException("unknown CurrentAlgo", nameof(passwordInfo));

            var request = await TaskExceptionWrapper.Wrap(() =>
                PasswordCheckHelper.GenRequest(passwordInfo, algo, password)
            ).ConfigureAwait(false);
            var res = await _tg.Call(request).ConfigureAwait(false);
            return res.Default!.User.Default!;
        }

        public async Task<User> CheckPassword(string password) {
            var ss = new SecureString();
            foreach (var x in password) {
                ss.AppendChar(x);
            }
            return await CheckPassword(ss).ConfigureAwait(false);
        }

        public async Task<User.DefaultTag> SignUp(
            string phoneNumber,
            string phoneCodeHash,
            string firstName,
            string lastName
        ) {
            var res = await _tg.Call(new SignUp(
                phoneNumber: phoneNumber,
                phoneCodeHash: phoneCodeHash,
                firstName: firstName,
                lastName: lastName
            )).ConfigureAwait(false);
            return res.Default!.User.Default!;
        }

        public async Task SignOut() {
            await _tg.Call(new LogOut()).ConfigureAwait(false);
        }
    }
}
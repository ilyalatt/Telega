using System;
using System.Security;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Telega.Connect;
using Telega.Rpc.Dto.Functions.Account;
using Telega.Rpc.Dto.Functions.Auth;
using Telega.Rpc.Dto.Types;
using Telega.Rpc.Dto.Types.Account;

namespace Telega.Client {
    public sealed class TelegramClientAuth {
        readonly ILogger _logger;
        readonly TgBellhop _tg;

        internal TelegramClientAuth(
            ILogger logger,
            TgBellhop tg
        ) {
            _logger = logger;
            _tg = tg;
        }


        User SetAuthorized(User user) {
            _logger.LogTrace("Authorized: " + user);
            _tg.SetSession(x => x.With(isAuthorized: true));
            return user;
        }

        public bool IsAuthorized =>
            _tg.Session.IsAuthorized;


        public async Task<string> SendCode(string apiHash, string phoneNumber) {
            var res = await _tg.Call(new SendCode(
                phoneNumber: phoneNumber,
                apiId: _tg.Session.ApiId,
                apiHash: apiHash,
                new CodeSettings(
                    allowFlashcall: false,
                    currentNumber: false,
                    allowAppHash: false,
                    allowMissedCall: false,
                    allowFirebase: false,
                    logoutTokens: null,
                    token: null,
                    appSandbox: false
                )
            )).ConfigureAwait(false);
            return res.Match(
                    defaultTag: x => x.PhoneCodeHash,
                    success_Tag: _ => throw new NotImplementedException()
            );
        }

        public async Task<User> SignIn(string phoneNumber, string phoneCodeHash, string code) {
            var res = await _tg.Call(new SignIn(
                phoneNumber: phoneNumber,
                phoneCodeHash: phoneCodeHash,
                phoneCode: code,
                emailVerification: null
            )).ConfigureAwait(false);

            return SetAuthorized(res.Default!.User);
        }

        public async Task<Password> GetPasswordInfo() =>
            await _tg.Call(new GetPassword()).ConfigureAwait(false);

        public async Task<User> CheckPassword(SecureString password) {
            var passwordInfo = await GetPasswordInfo().ConfigureAwait(false);
            if (!passwordInfo.HasPassword) {
                throw new ArgumentException("the account does not have a password", nameof(passwordInfo));
            }

            var currentAlgo = passwordInfo.CurrentAlgo
                ?? throw new ArgumentException("there is no CurrentAlgo", nameof(passwordInfo));
            var algo = currentAlgo
               .Sha256Sha256Pbkdf2Hmacsha512Iter100000Sha256ModPow_
               ?? throw new ArgumentException("unknown CurrentAlgo", nameof(passwordInfo));

            var request = await TaskWrapper.Wrap(() =>
                PasswordCheckHelper.GenRequest(passwordInfo, algo, password)
            ).ConfigureAwait(false);
            var res = await _tg.Call(request).ConfigureAwait(false);
            return SetAuthorized(res.Default!.User);
        }

        public async Task<User> CheckPassword(string password) {
            var ss = new SecureString();
            foreach (var x in password) {
                ss.AppendChar(x);
            }
            return await CheckPassword(ss).ConfigureAwait(false);
        }

        public async Task<User> SignUp(
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
            return SetAuthorized(res.Default!.User);
        }
    }
}
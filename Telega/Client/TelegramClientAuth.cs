using System;
using System.Linq;
using System.Threading.Tasks;
using LanguageExt;
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


        public async Task<string> SendCode(Some<string> apiHash, Some<string> phoneNumber) {
            var res = await _tg.Call(new SendCode(
                phoneNumber: phoneNumber,
                apiId: _tg.Session.ApiId,
                apiHash: apiHash,
                new CodeSettings(
                    allowFlashcall: false,
                    currentNumber: false,
                    allowAppHash: false
                )
            )).ConfigureAwait(false);
            return res.PhoneCodeHash;
        }

        public async Task<User> SignIn(Some<string> phoneNumber, Some<string> phoneCodeHash, Some<string> code) {
            var res = await _tg.Call(new SignIn(
                phoneNumber: phoneNumber,
                phoneCodeHash: phoneCodeHash,
                phoneCode: code
            )).ConfigureAwait(false);

            return SetAuthorized(res.AsTag().Single().User);
        }

        public async Task<Password> GetPasswordInfo() =>
            await _tg.Call(new GetPassword());

        public async Task<User> CheckPassword(Some<Password> passwordInfo, Some<string> passwordStr) {
            var pwdInfo = passwordInfo.Value;
            if (!pwdInfo.HasPassword) {
                throw new ArgumentException("the account does not have a password", nameof(pwdInfo));
            }

            var algo = pwdInfo.CurrentAlgo
               .IfNone(() => throw new ArgumentException("there is no CurrentAlgo", nameof(passwordInfo)))
               .AsSha256Sha256Pbkdf2Hmacsha512Iter100000Sha256ModPowTag()
               .IfNone(() => throw new ArgumentException("unknown CurrentAlgo", nameof(passwordInfo)));

            var request = await TaskWrapper.Wrap(() =>
                PasswordCheckHelper.GenRequest(pwdInfo, algo, passwordStr)
            ).ConfigureAwait(false);
            var res = await _tg.Call(request).ConfigureAwait(false);
            return SetAuthorized(res.AsTag().Single().User);
        }

        public async Task<User> SignUp(
            Some<string> phoneNumber,
            Some<string> phoneCodeHash,
            Some<string> firstName,
            Some<string> lastName
        ) {
            var res = await _tg.Call(new SignUp(
                phoneNumber: phoneNumber,
                phoneCodeHash: phoneCodeHash,
                firstName: firstName,
                lastName: lastName
            )).ConfigureAwait(false);
            return SetAuthorized(res.AsTag().Single().User);
        }
    }
}
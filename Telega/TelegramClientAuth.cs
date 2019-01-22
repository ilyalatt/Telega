using System;
using System.Threading.Tasks;
using LanguageExt;
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

        public async Task<User> CheckPassword(Some<Password> passwordInfo, Some<string> passwordStr)
        {
            var pwdInfo = passwordInfo.Value;
            if (!pwdInfo.HasPassword) throw new ArgumentException("the account does not have a password", nameof(pwdInfo));

            var algo = pwdInfo.CurrentAlgo
                .IfNone(() => throw new ArgumentException("there is no CurrentAlgo", nameof(passwordInfo)))
                .AsSha256Sha256Pbkdf2Hmacsha512Iter100000Sha256ModPowTag()
                .IfNone(() => throw new ArgumentException("unknown CurrentAlgo", nameof(passwordInfo)));

            var request = PasswordCheckHelper.GenRequest(pwdInfo, algo, passwordStr);
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

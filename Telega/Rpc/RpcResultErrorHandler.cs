using System;
using System.Text.RegularExpressions;
using LanguageExt;
using Telega.Rpc.Dto.Types;
using static LanguageExt.Prelude;

namespace Telega.Rpc
{
    class TgRpcResultUnknownErrorException : TgRpcException
    {
        public int ErrorCode { get; }
        public string ErrorMessage { get; }

        public TgRpcResultUnknownErrorException(int errorCode, Some<string> errorMessage) : base(
            $"Unknown rpc error ({errorCode}, '{errorMessage}').",
            None
        ) {
            ErrorCode = errorCode;
            ErrorMessage = errorMessage;
        }
    }

    static class RpcResultErrorHandler
    {
        public static TgException ToException(RpcError error)
        {
            var code = error.ErrorCode;
            var msg = error.ErrorMessage;

            // TODO: get some of these messages and simplify the extraction
            // I guess we can get a last index of '_' and parse a number after that
            int ExtractInt() =>
                Regex.Match(msg, @"\d+").Value.Apply(int.Parse);

            TimeSpan ExtractTimeSpan() =>
                ExtractInt().Apply(x => (double) x).Apply(TimeSpan.FromSeconds);

            TgDataCenterMigrationException ExtractDcMigration(DcMigrationReason reason) =>
                new TgDataCenterMigrationException(reason, ExtractInt());

            if (msg.StartsWith("FLOOD_WAIT_")) return new TgFloodException(ExtractTimeSpan());

            if (msg.StartsWith("PHONE_MIGRATE_")) return ExtractDcMigration(DcMigrationReason.Phone);
            if (msg.StartsWith("FILE_MIGRATE_")) return ExtractDcMigration(DcMigrationReason.File);
            if (msg.StartsWith("USER_MIGRATE_")) return ExtractDcMigration(DcMigrationReason.User);
            if (msg.StartsWith("NETWORK_MIGRATE_")) return ExtractDcMigration(DcMigrationReason.Network);

            if (msg == "PHONE_CODE_INVALID") return new TgInvalidPhoneCodeException();
            if (msg == "PHONE_NUMBER_UNOCCUPIED") return new TgPhoneNumberUnoccupiedException(); // 400
            if (msg == "SESSION_PASSWORD_NEEDED") return new TgPasswordNeededException();

            if (msg == "AUTH_KEY_UNREGISTERED") throw new TgNotAuthenticatedException();

            return new TgRpcResultUnknownErrorException(code, msg);
        }
    }
}

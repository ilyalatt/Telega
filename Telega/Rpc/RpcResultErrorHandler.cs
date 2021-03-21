using System;
using System.Text.RegularExpressions;
using LanguageExt;
using Telega.Rpc.Dto.Types;
using static LanguageExt.Prelude;

namespace Telega.Rpc {
    class TgRpcResultUnknownErrorException : TgRpcException {
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

    static class RpcResultErrorHandler {
        public static TgException ToException(RpcError error) {
            var code = error.ErrorCode;
            var msg = error.ErrorMessage;

            // TODO: get some of these messages and simplify the extraction
            // I guess we can get a last index of '_' and parse a number after that
            int ExtractInt() =>
                Regex.Match(msg, @"\d+").Value.Apply(int.Parse);

            TimeSpan ExtractTimeSpan() =>
                ExtractInt().Apply(x => (double) x).Apply(TimeSpan.FromSeconds);

            TgDataCenterMigrationException DcMigration(DcMigrationReason reason) =>
                new(reason, ExtractInt());

            return msg switch {
                var x when x.StartsWith("FLOOD_WAIT_") => new TgFloodException(ExtractTimeSpan()),
                var x when x.StartsWith("PHONE_MIGRATE_") => DcMigration(DcMigrationReason.Phone),
                var x when x.StartsWith("FILE_MIGRATE_") => DcMigration(DcMigrationReason.File),
                var x when x.StartsWith("USER_MIGRATE_") => DcMigration(DcMigrationReason.User),
                var x when x.StartsWith("NETWORK_MIGRATE_") => DcMigration(DcMigrationReason.Network),
                "PHONE_CODE_INVALID" => new TgInvalidPhoneCodeException(),
                "PASSWORD_HASH_INVALID" => new TgInvalidPasswordException(),
                "PHONE_NUMBER_UNOCCUPIED" => new TgPhoneNumberUnoccupiedException(),
                "SESSION_PASSWORD_NEEDED" => new TgPasswordNeededException(),
                "AUTH_KEY_UNREGISTERED" => throw new TgNotAuthenticatedException(),
                _ => new TgRpcResultUnknownErrorException(code, msg)
            };
        }
    }
}
using System;
using LanguageExt;
using Telega.Rpc;
using static LanguageExt.Prelude;

namespace Telega
{
    public abstract class TgException : Exception
    {
        internal TgException(Some<string> message, Option<Exception> innerException) : base(
            message,
            innerException.IfNoneUnsafe(() => null!)
        ) { }
    }

    public class TgTransportException : TgException
    {
        internal TgTransportException(
            Some<string> message,
            Option<Exception> innerException
        ) : base(message, innerException) { }
    }

    public sealed class TgBrokenConnectionException : TgTransportException
    {
        internal TgBrokenConnectionException() : base(
            "The connection is closed.",
            None
        ) { }
    }

    sealed class TgProtocolViolation : TgInternalException
    {
        public TgProtocolViolation() : base("The protocol is violated.", None) { }
    }

    public sealed class TgNotAuthenticatedException : TgException
    {
        public TgNotAuthenticatedException() : base("Authentication is required.", None) { }
    }

    public sealed class TgFloodException : TgException
    {
        public TimeSpan Delay { get; }

        // TODO
        internal TgFloodException(TimeSpan delay) : base(
            $"Flood prevention. Wait {delay.TotalMinutes} minutes.",
            None
        ) => Delay = delay;
    }

    enum DcMigrationReason
    {
        Phone,
        File,
        User,
        Network
    }

    class TgInternalException : TgException
    {
        internal TgInternalException(
            Some<string> additionalMessage,
            Option<Exception> innerException
        ) : base(
            "Telega internal exception. " + additionalMessage,
            innerException
        ) { }
    }

    class TgFailedAssertionException : TgInternalException
    {
        public TgFailedAssertionException(Some<string> msg) : base($"Assert failed. {msg}", None) { }
    }

    sealed class TgDataCenterMigrationException : TgInternalException
    {
        public DcMigrationReason Reason { get; }
        public int Dc { get; }

        internal TgDataCenterMigrationException(DcMigrationReason reason, int dc) : base("Data center migration.", None)
        {
            Reason = reason;
            Dc = dc;
        }
    }

    sealed class TgBadSaltException : TgInternalException
    {
        public TgBadSaltException() : base("bad_server_salt is received.", None) { }
    }

    public sealed class TgInvalidPhoneCodeException : TgException
    {
        internal TgInvalidPhoneCodeException() : base(
            "The numeric code used to authenticate does not match the numeric code sent by SMS/Telegram.",
            None
        ) { }
    }

    public sealed class TgPhoneNumberUnoccupiedException : TgException
    {
        internal TgPhoneNumberUnoccupiedException() : base(
            "The phone number is not occupied.",
            None
        ) { }
    }

    public sealed class TgPasswordNeededException : TgException
    {
        internal TgPasswordNeededException() : base("This account has a password.", None) { }
    }
}

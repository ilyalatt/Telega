using System;

namespace Telega {
    public abstract class TgException : Exception {
        internal TgException(string message, Exception? innerException) : base(
            message,
            innerException
        ) { }
    }

    public class TgTransportException : TgException {
        internal TgTransportException(
            string message,
            Exception? innerException
        ) : base(message, innerException) { }
    }

    public sealed class TgBrokenConnectionException : TgTransportException {
        internal TgBrokenConnectionException() : base(
            "The connection is closed.",
            null
        ) { }
    }

    sealed class TgProtocolViolation : TgInternalException {
        public TgProtocolViolation() : base("The protocol is violated.", null) { }
    }

    public sealed class TgNotAuthenticatedException : TgException {
        public TgNotAuthenticatedException() : base("Authentication is required.", null) { }
    }

    public sealed class TgFloodException : TgException {
        public TimeSpan Delay { get; }

        // TODO
        internal TgFloodException(TimeSpan delay) : base(
            $"Flood prevention. Wait {delay.TotalMinutes} minutes.",
            null
        ) => Delay = delay;
    }

    enum DcMigrationReason {
        Phone,
        File,
        User,
        Network
    }

    class TgInternalException : TgException {
        internal TgInternalException(
            string additionalMessage,
            Exception? innerException
        ) : base(
            "Telega internal exception. " + additionalMessage,
            innerException
        ) { }
    }

    class TgFailedAssertionException : TgInternalException {
        public TgFailedAssertionException(string msg) : base($"Assert failed. {msg}", null) { }
    }

    sealed class TgDataCenterMigrationException : TgInternalException {
        public DcMigrationReason Reason { get; }
        public int Dc { get; }

        internal TgDataCenterMigrationException(DcMigrationReason reason, int dc) : base("Data center migration.", null) {
            Reason = reason;
            Dc = dc;
        }
    }

    sealed class TgBadSaltException : TgInternalException {
        public TgBadSaltException() : base("bad_server_salt is received.", null) { }
    }

    public sealed class TgInvalidPhoneCodeException : TgException {
        internal TgInvalidPhoneCodeException() : base(
            "The numeric code used to authenticate does not match the numeric code sent by SMS/Telegram.",
            null
        ) { }
    }

    public sealed class TgInvalidPasswordException : TgException {
        internal TgInvalidPasswordException() : base(
            "The provided password is invalid.",
            null
        ) { }
    }

    public sealed class TgPhoneNumberUnoccupiedException : TgException {
        internal TgPhoneNumberUnoccupiedException() : base(
            "The phone number is not occupied.",
            null
        ) { }
    }

    public sealed class TgPasswordNeededException : TgException {
        internal TgPasswordNeededException() : base("This account has a password.", null) { }
    }
}
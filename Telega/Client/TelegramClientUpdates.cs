using System;
using Telega.Connect;
using Telega.Rpc.Dto.Types;

namespace Telega.Client {
    public sealed class TelegramClientUpdates {
        readonly TgBellhop _tg;
        public IObservable<UpdatesType> Stream { get; }
        public IObservable<Exception> Exceptions { get; }

        internal TelegramClientUpdates(TgBellhop tg) {
            _tg = tg;
            Stream = _tg.Updates;
            Exceptions = _tg.Exceptions;
        }
    }
}
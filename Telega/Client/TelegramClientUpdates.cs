using System;
using Telega.Connect;
using Telega.Rpc.Dto.Types;

namespace Telega.Client {
    public sealed class TelegramClientUpdates {
        readonly TgBellhop _tg;
        public IObservable<UpdatesType> Stream { get; }

        internal TelegramClientUpdates(TgBellhop tg) {
            _tg = tg;
            Stream = _tg.Updates;
        }
    }
}
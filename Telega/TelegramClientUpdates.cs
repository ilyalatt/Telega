using System;
using LanguageExt;
using Telega.Connect;
using Telega.Rpc.Dto.Types;

namespace Telega {
    public sealed class TelegramClientUpdates {
        readonly TgBellhop _tg;
        public IObservable<UpdatesType> Stream { get; }

        internal TelegramClientUpdates(Some<TgBellhop> tg) {
            _tg = tg;
            Stream = _tg.Updates;
        }
    }
}
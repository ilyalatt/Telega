using System;
using LanguageExt;

namespace Telega.Rpc.Dto.Generator.TgScheme {
    class TgSchemeParserException : Exception {
        public TgSchemeParserException(Some<string> message) : base(message) { }
    }
}
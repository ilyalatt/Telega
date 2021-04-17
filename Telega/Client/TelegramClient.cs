using System;
using System.Threading.Tasks;
using Telega.Connect;
using Telega.Rpc.Dto;
using Telega.Session;

namespace Telega.Client {
    public sealed class TelegramClient : IDisposable {
        readonly TelegramClientConfig _config;
        readonly TgBellhop _bellhop;

        public TelegramClientAuth Auth { get; }
        public TelegramClientContacts Contacts { get; }
        public TelegramClientChannels Channels { get; }
        public TelegramClientMessages Messages { get; }
        public TelegramClientUpload Upload { get; }
        public TelegramClientUpdates Updates { get; }

        public TelegramClient(
            TelegramClientConfig? config = default
        ) {
            _config = config ?? TelegramClientConfig.Default;
            _bellhop = new TgBellhop(_config);

            Auth = new TelegramClientAuth(_bellhop);
            Contacts = new TelegramClientContacts(_bellhop);
            Channels = new TelegramClientChannels(_bellhop);
            Messages = new TelegramClientMessages(_bellhop);
            Upload = new TelegramClientUpload(_bellhop);
            Updates = new TelegramClientUpdates(_bellhop);
        }

        public async Task Connect(
            Func<TgRpcConfig, TgRpcConfig>? configurator = null
        ) {
            var config = _bellhop.Session?.RpcConfig ?? TgRpcConfig.Default;
            config = configurator != null ? configurator(config) : config;
            await _bellhop.Connect(config).ConfigureAwait(false);
        }

        public void Dispose() =>
            _bellhop.Dispose();

        public Task<T> Call<T>(ITgFunc<T> func) =>
            _bellhop.Call(func);
    }
}
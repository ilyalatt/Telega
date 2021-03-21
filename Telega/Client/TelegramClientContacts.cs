using System.Threading.Tasks;
using LanguageExt;
using Telega.Connect;
using Telega.Rpc.Dto.Functions.Contacts;
using Telega.Rpc.Dto.Types.Contacts;

namespace Telega.Client {
    public sealed class TelegramClientContacts {
        readonly TgBellhop _tg;
        internal TelegramClientContacts(Some<TgBellhop> tg) => _tg = tg;


        public async Task<Contacts> GetContacts() =>
            await _tg.Call(new GetContacts(hash: 0)).ConfigureAwait(false);

        public async Task<Found> Search(
            Some<string> q,
            int limit = 10
        ) =>
            await _tg.Call(new Search(
                q: q,
                limit: limit
            )).ConfigureAwait(false);

        public async Task<ResolvedPeer> ResolveUsername(
            Some<string> username
        ) =>
            await _tg.Call(new ResolveUsername(
                username: username
            )).ConfigureAwait(false);
    }
}
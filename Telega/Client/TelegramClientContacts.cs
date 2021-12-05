using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NullExtensions;
using Telega.Connect;
using Telega.Rpc.Dto.Functions.Contacts;
using Telega.Rpc.Dto.Types;
using Telega.Rpc.Dto.Types.Contacts;

namespace Telega.Client {
    public sealed class TelegramClientContacts {
        readonly TgBellhop _tg;
        internal TelegramClientContacts(TgBellhop tg) => _tg = tg;


        public async Task<Contacts> GetContacts() =>
            await _tg.Call(new GetContacts(hash: 0)).ConfigureAwait(false);

        public async Task<Found> Search(
            string q,
            int limit = 10
        ) =>
            await _tg.Call(new Search(
                q: q,
                limit: limit
            )).ConfigureAwait(false);

        public async Task<ResolvedPeer> ResolveUsername(
            string username
        ) =>
            await _tg.Call(new ResolveUsername(
                username: username
            )).ConfigureAwait(false);

        public async Task<IReadOnlyList<(int userIdx, User.DefaultTag user)>> ImportUsers(
            IEnumerable<(string phone, string firstName, string lastName)> users
        ) {
            var resp = await _tg.Call(new ImportContacts(
                contacts: users.Select((user, userIdx) => new InputContact(
                    clientId: userIdx,
                    phone: user.phone,
                    firstName: user.firstName,
                    lastName: user.lastName
                )).ToList()
            )).ConfigureAwait(false);
            var usersMap = resp.Users.NChoose(x => x.Default).ToDictionary(x => x.Id);
            return resp.Imported.Select(x => ((int) x.ClientId, usersMap[x.UserId])).ToList();
        }
    }
}
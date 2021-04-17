using System.Threading.Tasks;
using Telega.Utils;

namespace Telega.Session.Store {
    static class SessionVarBinding {
        public static async Task<Var<TgSession>> Bind(ISessionStore sessionStore) {
            var session = await sessionStore.Get().ConfigureAwait(false);
            var v = new Var<TgSession>(session);
            v.Subscribe(sessionStore.Update);
            return v;
        }
    }
}
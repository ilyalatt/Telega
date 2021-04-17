using System.Threading.Tasks;

namespace Telega.Session.Store {
    public sealed class InMemorySessionStore : ISessionStore {
        TgSession _session = TgSession.Default;
        
        public void Dispose() { }

        public Task<TgSession> Get() =>
            Task.FromResult(_session);

        public void Update(TgSession session) =>
            _session = session;
    }
}
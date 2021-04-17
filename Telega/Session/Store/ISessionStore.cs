using System;
using System.Threading.Tasks;

namespace Telega.Session.Store {
    public interface ISessionStore : IDisposable {
        Task<TgSession> Get();
        void Update(TgSession session);
    }
}
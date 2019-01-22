using System.Threading.Tasks;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Telega
{
    class FakeSessionStore : ISessionStore
    {
        public Task<Option<Session>> Load() => Task.FromResult((Option<Session>) None);
        public Task Save(Some<Session> someSession) => Task.CompletedTask;
    }
}

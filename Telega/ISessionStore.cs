using System.Threading.Tasks;

namespace Telega {
    public interface ISessionStore {
        Task<Session?> Load();
        Task Save(Session session);
    }
}
using System.Threading.Tasks;
using Telega.Rpc.Dto;

namespace Telega.CallMiddleware
{
    public delegate Task<Task<T>> TgCallHandler<T>(ITgFunc<T> func);
}

using System;
using System.Threading.Tasks;

namespace Telega.CallMiddleware
{
    sealed class FloodMiddleware : ITgCallMiddleware
    {
        DateTime _unlockTimestamp;

        public TgCallHandler<T> Handle<T>(TgCallHandler<T> next) => async func =>
        {
            var lockSpan = _unlockTimestamp - DateTime.Now;
            if (lockSpan > TimeSpan.Zero) throw new TgFloodException(lockSpan);

            var receive = await next(func);

            async Task<T> ReceiveWrapper()
            {
                try
                {
                    return await receive;
                }
                catch (TgFloodException e)
                {
                    _unlockTimestamp = DateTime.Now + e.Delay;
                    throw;
                }
            }

            return ReceiveWrapper();
        };
    }
}

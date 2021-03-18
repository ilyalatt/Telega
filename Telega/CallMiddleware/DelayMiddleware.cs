using System;
using System.Threading.Tasks;
using Telega.Rpc.Dto;
using Telega.Rpc.Dto.Functions.Upload;
using Telega.Utils;

namespace Telega.CallMiddleware
{
    // TODO: DC-specific via context
    sealed class DelayMiddleware : ITgCallMiddleware
    {
        public int MinMsDelay { get; }
        public int MaxMsDelay { get; }

        readonly TaskQueue _taskQueue = new TaskQueue();
        DateTime _lastReqTimestamp;

        public DelayMiddleware(int minMsDelay, int maxMsDelay)
        {
            MinMsDelay = minMsDelay;
            MaxMsDelay = maxMsDelay;
        }

        public DelayMiddleware() : this(700, 1200) { }

        static bool IsIgnored<T>(ITgFunc<T> func) =>
            func is GetFile ||
            func is GetCdnFile ||
            func is GetWebFile ||
            func is SaveFilePart ||
            func is SaveBigFilePart ||
            func is GetFileHashes ||
            func is ReuploadCdnFile ||
            func is GetCdnFileHashes;

        public TgCallHandler<T> Handle<T>(TgCallHandler<T> next) => func => _taskQueue.Put(async () =>
        {
            var timeSinceLastReq = DateTime.Now - _lastReqTimestamp;
            var isDelayNeeded = timeSinceLastReq.TotalMilliseconds < MaxMsDelay && !IsIgnored(func);

            if (isDelayNeeded) await Task.Delay(Rnd.NextInt32(MinMsDelay, MaxMsDelay));
            _lastReqTimestamp = DateTime.Now;

            return await next(func);
        });
    }
}

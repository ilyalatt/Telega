using System;
using System.Threading;
using System.Threading.Tasks;

namespace Telega.Utils
{
    class TaskQueue
    {
        readonly SemaphoreSlim _queue = new(1, 1);

        public async Task<T> Put<T>(Func<Task<T>> func)
        {
            await _queue.WaitAsync();
            try
            {
                return await func();
            }
            finally
            {
                _queue.Release(1);
            }
        }

        public async Task Put(Func<Task> func)
        {
            await _queue.WaitAsync();
            try
            {
                await func();
            }
            finally
            {
                _queue.Release(1);
            }
        }
    }
}

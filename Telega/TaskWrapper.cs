using System;
using System.Threading.Tasks;

namespace Telega
{
    static class TaskWrapper
    {
        static bool ShouldWrapExc(Exception exc) =>
            !(exc is TgException) && !(exc is OutOfMemoryException);

        static Exception WrapExc(Exception exc) =>
            new TgInternalException("Unhandled exception. See an inner exception.", exc);

        public static async Task<T> Wrap<T>(Func<Task<T>> wrapper)
        {
            try
            {
                return await Task.Run(wrapper).ConfigureAwait(false);
            }
            catch (Exception exc) when (ShouldWrapExc(exc))
            {
                throw WrapExc(exc);
            }
        }

        public static async Task<T> Wrap<T>(Func<T> wrapper)
        {
            try
            {
                return await Task.Run(wrapper).ConfigureAwait(false);
            }
            catch (Exception exc) when (ShouldWrapExc(exc))
            {
                throw WrapExc(exc);
            }
        }
    }
}

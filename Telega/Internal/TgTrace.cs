using System;

namespace Telega.Internal
{
    // so ugly
    public static class TgTrace
    {
        public static bool IsEnabled { get; set; }

        public static void Trace(string msg)
        {
            if (!IsEnabled) return;
            Console.WriteLine(msg);
        }
    }
}

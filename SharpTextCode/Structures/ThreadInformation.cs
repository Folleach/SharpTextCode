using System;
using System.Threading;

namespace SharpTextCode.Structures
{
    public struct ThreadInformation
    {
        public Thread Thr;
        public DateTime StartTime;

        public ThreadInformation(Thread thread)
        {
            Thr = thread;
            StartTime = DateTime.UtcNow;
        }
    }
}

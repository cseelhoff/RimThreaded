using System;
using System.Threading;

namespace RimThreaded
{
    public class ThreadedTickList
    {
        public Action prepareAction;
        public Action tickAction;
        public int preparing = -1;
        public int threadCount = -1;
        public bool readyToTick = false;
        public EventWaitHandle prepEventWaitStart = new ManualResetEvent(false);
    }
}

using System;

namespace RimThreaded
{
    public class ThreadedTickList
    {
        public Action prepareAction;
        public Func<bool> tickAction;
        public int preparing = -1;
        public bool readyToTick = false;
    }
}

using UnityEngine;
using System.Threading;
using UnityEngine.Experimental.Rendering;
using System;
using System.Reflection;

namespace RimThreaded
{

    public class GraphicDatabaseHeadRecords_Patch
    {

        public static bool BuildDatabaseIfNecessary()
        {
            int tID = Thread.CurrentThread.ManagedThreadId;
            if (RimThreaded.mainRequestWaits.TryGetValue(tID, out EventWaitHandle eventWaitStart))
            {
                lock (RimThreaded.buildDatabase)
                {
                    RimThreaded.buildDatabase.Enqueue(tID);
                }
                RimThreaded.mainThreadWaitHandle.Set();
                eventWaitStart.WaitOne();
                return false;
            }
            return true;
        }
    }
}

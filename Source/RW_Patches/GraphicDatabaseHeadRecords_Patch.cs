using System;
using Verse;
using static RimThreaded.RimThreaded;
using static System.Threading.Thread;

namespace RimThreaded.RW_Patches
{

    public class GraphicDatabaseHeadRecords_Patch
    {

        internal static void RunDestructivePatches()
        {
            //Type original = typeof(GraphicDatabaseHeadRecords);
            //Type patched = typeof(GraphicDatabaseHeadRecords_Patch);
            //RimThreadedHarmony.Prefix(original, patched, nameof(BuildDatabaseIfNecessary));
        }

        /*
        private static readonly Action<object[]> SafeFunction = parameters =>
            GraphicDatabaseHeadRecords.BuildDatabaseIfNecessary();

        public static bool BuildDatabaseIfNecessary()
        {
            if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out ThreadInfo threadInfo))
                return true;
            threadInfo.safeFunctionRequest = new object[] { SafeFunction, new object[] { } };
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return false;
        }
        */
    }
}

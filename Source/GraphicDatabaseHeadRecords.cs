using System;
using System.Reflection;
using HarmonyLib;
using Verse;
using static RimThreaded.RimThreaded;
using static System.Threading.Thread;

namespace RimThreaded
{

    public class GraphicDatabaseHeadRecords_Patch
    {

        internal static void RunDestructivePatches()
        {
            Type original = typeof(GraphicDatabaseHeadRecords);
            Type patched = typeof(GraphicDatabaseHeadRecords_Patch);
            RimThreadedHarmony.Prefix(original, patched, "BuildDatabaseIfNecessary");
        }
        
        static readonly MethodInfo reflectionMethod = AccessTools.Method(typeof(GraphicDatabaseHeadRecords), "BuildDatabaseIfNecessary");

        static readonly Action buildDatabaseIfNecessary =
            (Action)Delegate.CreateDelegate
            (typeof(Action), reflectionMethod);

        static readonly Action<object[]> safeFunction = parameters =>
            buildDatabaseIfNecessary();

        public static bool BuildDatabaseIfNecessary()
        {
            if (!CurrentThread.IsBackground || !allThreads2.TryGetValue(CurrentThread, out ThreadInfo threadInfo)) 
                return true;
            threadInfo.safeFunctionRequest = new object[] { safeFunction, new object[] { } };
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return false;
        }

    }
}

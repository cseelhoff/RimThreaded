using System;
using UnityEngine;
using Verse;
using static RimThreaded.RimThreaded;
using static System.Threading.Thread;

namespace RimThreaded.RW_Patches
{
    class Text_Patch
    {

        private static readonly Type Original = typeof(Text);
        private static readonly Type Patched = typeof(Text_Patch);

        public static void RunDestructivePatches()
        {
            RimThreadedHarmony.Prefix(Original, Patched, "get_CurFontStyle");
        }

        public static Func<object[], object> safeFunction = parameters => { return Text.CurFontStyle; };
        public static bool get_CurFontStyle(ref GUIStyle __result)
        {
            if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out ThreadInfo threadInfo))
                return true;
            threadInfo.safeFunctionRequest = new object[] { safeFunction, new object[] { } };
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            __result = (GUIStyle)threadInfo.safeFunctionResult;
            return false;

        }
    }
}

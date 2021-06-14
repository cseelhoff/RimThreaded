using System;
using static System.Threading.Thread;
using static RimThreaded.RimThreaded;

namespace RimThreaded
{
    class UnityEngine_Object_Patch
    {

        internal static void RunDestructivePatches()
        {
            Type original = typeof(UnityEngine.Object);
            Type patched = typeof(UnityEngine_Object_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(ToString), new Type[] { });
            RimThreadedHarmony.Prefix(original, patched, nameof(Destroy), new Type[] { });
        }

        public static bool ToString(UnityEngine.Object __instance, ref string __result)
        {
            if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out ThreadInfo threadInfo))
            {
                return true;
            }
            Func<object[], object> safeFunction = parameters => __instance.ToString();
            threadInfo.safeFunctionRequest = new object[] { safeFunction, new object[] { } };
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            __result = (string)threadInfo.safeFunctionResult;
            return false;
        }
        public static bool Destroy(UnityEngine.Object __instance)
        {
            if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out ThreadInfo threadInfo))
            {
                return true;
            }
            Action<object[]> safeActionDestroy = parameters => UnityEngine.Object.Destroy(__instance);
            threadInfo.safeFunctionRequest = new object[] { safeActionDestroy, new object[] { } };
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return false;
        }
    }
}

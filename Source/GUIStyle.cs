using UnityEngine;
using System;
using static RimThreaded.RimThreaded;
using static System.Threading.Thread;

namespace RimThreaded
{

    public class GUIStyle_Patch
    {
        public static bool CalcHeight(GUIStyle __instance, ref float __result, GUIContent content, float width)
        {
            if (allThreads2.TryGetValue(CurrentThread, out ThreadInfo threadInfo))
            {
                Func<object[], object> safeFunction = p => __instance.CalcHeight((GUIContent)p[0], (float)p[1]);
                threadInfo.safeFunctionRequest = new object[] { safeFunction, new object[] { content, width } };
                mainThreadWaitHandle.Set();
                threadInfo.eventWaitStart.WaitOne();
                __result = (float)threadInfo.safeFunctionResult;
                return false;
            }
            return true;
        }
        public static bool CalcSize(GUIStyle __instance, ref Vector2 __result, GUIContent content)
        {
            if (allThreads2.TryGetValue(CurrentThread, out ThreadInfo threadInfo))
            {
                Func<object[], object> safeFunction = p => __instance.CalcSize((GUIContent)p[0]);
                threadInfo.safeFunctionRequest = new object[] { safeFunction, new object[] { content } };
                mainThreadWaitHandle.Set();
                threadInfo.eventWaitStart.WaitOne();
                __result = (Vector2)threadInfo.safeFunctionResult;
                return false;
            }
            return true;
        }

        internal static void RunDestructivePatches()
        {
            Type original = typeof(GUIStyle);
            Type patched = typeof(GUIStyle_Patch);
            RimThreadedHarmony.Prefix(original, patched, "CalcHeight");
            RimThreadedHarmony.Prefix(original, patched, "CalcSize");
        }
    }
}

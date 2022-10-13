using System;
using UnityEngine;
using static RimThreaded.RimThreaded;
using static System.Threading.Thread;
using static HarmonyLib.AccessTools;

namespace RimThreaded.RW_Patches
{

    public class GUIStyle_Patch
    {
        internal static void RunDestructivePatches()
        {
            Type original = typeof(GUIStyle);
            Type patched = typeof(GUIStyle_Patch);
            RimThreadedHarmony.Prefix(original, patched, "CalcHeight");
            RimThreadedHarmony.Prefix(original, patched, "CalcSize");
        }

        private static readonly Func<GUIStyle, GUIContent, float, float> FuncCalcHeight =
            (Func<GUIStyle, GUIContent, float, float>)Delegate.CreateDelegate(
                typeof(Func<GUIStyle, GUIContent, float, float>),
                Method(typeof(GUIStyle), "CalcHeight", new[]
                {
                    typeof(GUIContent), typeof(float)
                }));

        private static readonly Func<GUIStyle, GUIContent, Vector2> FuncCalcSize =
            (Func<GUIStyle, GUIContent, Vector2>)Delegate.CreateDelegate(
                typeof(Func<GUIStyle, GUIContent, Vector2>),
                Method(typeof(GUIStyle), "CalcSize", new[]
                {
                    typeof(GUIContent)
                }));

        public static bool CalcHeight(GUIStyle __instance, ref float __result, GUIContent content, float width)
        {
            if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out ThreadInfo threadInfo))
                return true;
            //Func<object[], object> FuncCalcHeight = p => __instance.CalcHeight((GUIContent)p[0], (float)p[1]);
            threadInfo.safeFunctionRequest = new object[] { FuncCalcHeight, new object[] { __instance, content, width } };
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            __result = (float)threadInfo.safeFunctionResult;
            return false;
        }
        public static bool CalcSize(GUIStyle __instance, ref Vector2 __result, GUIContent content)
        {
            if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out ThreadInfo threadInfo))
                return true;
            Func<object[], object> FuncCalcSize2 = p => __instance.CalcSize((GUIContent)p[1]);
            threadInfo.safeFunctionRequest = new object[] { FuncCalcSize2, new object[] { __instance, content } };
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            __result = (Vector2)threadInfo.safeFunctionResult;
            return false;
        }

    }
}

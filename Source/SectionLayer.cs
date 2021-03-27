using HarmonyLib;
using System;
using Verse;
using UnityEngine;
using static System.Threading.Thread;
using static RimThreaded.RimThreaded;

namespace RimThreaded
{

    public class SectionLayer_Patch
    {
        public static bool GetSubMesh(SectionLayer __instance, ref LayerSubMesh __result, Material material)
        {
            if (allThreads2.TryGetValue(CurrentThread, out ThreadInfo threadInfo))
            {
                Func<object[], object> safeFunction = parameters => __instance.GetSubMesh((Material)parameters[0]);
                threadInfo.safeFunctionRequest = new object[] { safeFunction, new object[] { material } };
                mainThreadWaitHandle.Set();
                threadInfo.eventWaitStart.WaitOne();
                __result = (LayerSubMesh)threadInfo.safeFunctionResult;
                return false;
            }
            return true;
        }

        internal static void RunDestructivePatches()
        {
            Type original = typeof(SectionLayer);
            Type patched = typeof(SectionLayer_Patch);
            RimThreadedHarmony.Prefix(original, patched, "GetSubMesh");
        }
    }
}

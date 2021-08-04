using RimWorld;
using System;
using System.Threading;
using UnityEngine;
using Verse;
using static RimThreaded.RimThreaded;

namespace RimThreaded
{
    class MoteBubble_Patch
    {
        internal static void RunDestructivePatches()
        {
            Type original = typeof(MoteBubble);
            Type patched = typeof(MoteBubble_Patch);
#if RW13
            RimThreadedHarmony.Prefix(original, patched, nameof(SetupMoteBubble));
#endif
        }
#if RW13
        public static bool SetupMoteBubble(MoteBubble __instance, Texture2D icon, Pawn target, Color? iconColor = null)
        {
            __instance.iconMat = MaterialPool.MatFrom(icon, ShaderDatabase.TransparentPostLight, Color.white);
            //__instance.iconMatPropertyBlock = new MaterialPropertyBlock();
            if (!allWorkerThreads.TryGetValue(Thread.CurrentThread, out ThreadInfo threadInfo))
            {
                __instance.iconMatPropertyBlock = new MaterialPropertyBlock();
            }
            else
            {
                Func<object[], object> FuncMaterialPropertyBlock = parameters => new MaterialPropertyBlock();
                threadInfo.safeFunctionRequest = new object[] { FuncMaterialPropertyBlock, new object[] { } };
                mainThreadWaitHandle.Set();
                threadInfo.eventWaitStart.WaitOne();
                __instance.iconMatPropertyBlock = (MaterialPropertyBlock)threadInfo.safeFunctionResult;
            }

            __instance.arrowTarget = target;
            if (!iconColor.HasValue)
                return false;
            __instance.iconMatPropertyBlock.SetColor("_Color", iconColor.Value);
            return false;
        }
#endif
    }
}

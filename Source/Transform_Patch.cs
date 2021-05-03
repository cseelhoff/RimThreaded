using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;
using static HarmonyLib.AccessTools;
using static RimThreaded.RimThreaded;
using static System.Threading.Thread;

namespace RimThreaded
{
    class Transform_Patch
    {

        private static readonly Func<Transform, Vector3> ActionGet_position =
            (Func<Transform, Vector3>)Delegate.CreateDelegate(
                typeof(Func<Transform, Vector3>),
                Method(typeof(Transform), "get_position", Type.EmptyTypes));

        private static readonly Func<object[], object> ActionGet_position2 = parameters =>
            ActionGet_position((Transform)parameters[0]);


        public static bool get_position(Transform __instance, ref Vector3 __result)
        {
            if (!allThreads2.TryGetValue(CurrentThread, out ThreadInfo threadInfo)) return true;
            threadInfo.safeFunctionRequest = new object[] { ActionGet_position2, new object[] { __instance } };
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            __result = (Vector3) threadInfo.safeFunctionResult;
            return false;
        }

        internal static void RunDestructivePatches()
        {
            Type original = typeof(Transform);
            Type patched = typeof(Transform_Patch);
            RimThreadedHarmony.Prefix(original, patched, "get_position");
        }
    }
}

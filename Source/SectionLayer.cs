using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using UnityEngine;
using System.Threading;

namespace RimThreaded
{

    public class SectionLayer_Patch
    {

        public static bool GetSubMesh(SectionLayer __instance, ref LayerSubMesh __result, Material material)
        {
            int tID = Thread.CurrentThread.ManagedThreadId;
            if (RimThreaded.mainRequestWaits.TryGetValue(tID, out EventWaitHandle eventWaitStart))
            {
                Func<object[], object> safeFunction = p => __instance.GetSubMesh((Material)p[0]);
                object[] functionAndParameters = new object[] { safeFunction, new object[] { material } };
                lock (RimThreaded.safeFunctionRequests)
                {
                    RimThreaded.safeFunctionRequests[tID] = functionAndParameters;
                }
                RimThreaded.mainThreadWaitHandle.Set();
                eventWaitStart.WaitOne();
                RimThreaded.safeFunctionResults.TryGetValue(tID, out object safeFunctionResult);
                __result = (LayerSubMesh)safeFunctionResult;
                return false;
            }
            return true;
        }

	}
}

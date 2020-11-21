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
                lock (RimThreaded.layerSubMeshRequests)
                {
                    RimThreaded.layerSubMeshRequests[tID] = new object[] { __instance, material };
                }
                RimThreaded.mainThreadWaitHandle.Set();
                eventWaitStart.WaitOne();
                RimThreaded.layerSubMeshResults.TryGetValue(tID, out LayerSubMesh layerSubMesh);
                __result = layerSubMesh;
                return false;
            }
            return true;
        }

	}
}

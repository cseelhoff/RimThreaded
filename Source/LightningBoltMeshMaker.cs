using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using System.Threading;
using UnityEngine;

namespace RimThreaded
{

    public class LightningBoltMeshMaker_Patch
    {
        public static bool NewBoltMesh(ref Mesh __result)
        {
            int tID = Thread.CurrentThread.ManagedThreadId;
            if (RimThreaded.mainRequestWaits.TryGetValue(tID, out EventWaitHandle eventWaitStart))
            {
                RimThreaded.newBoltMeshRequests.Enqueue(tID);
                RimThreaded.mainThreadWaitHandle.Set();
                eventWaitStart.WaitOne();
                RimThreaded.newBoltMeshResults.TryGetValue(tID, out Mesh newBoltMesh_result);
                __result = newBoltMesh_result;
                return false;
            }
            return true;
        }

    }
}

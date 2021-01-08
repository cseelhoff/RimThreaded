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
    /*
    public class Mesh_Patch
    {
		public static bool MeshSafe(ref Mesh __instance)
		{
            
            int tID = Thread.CurrentThread.ManagedThreadId;
            if (RimThreaded.mainRequestWaits.TryGetValue(tID, out EventWaitHandle eventWaitStart))
            {
                lock (RimThreaded.meshRequests)
                {
                    RimThreaded.meshRequests[tID] = __instance;
                }
                RimThreaded.mainThreadWaitHandle.Set();
                eventWaitStart.WaitOne();
                RimThreaded.meshResults.TryGetValue(tID, out Mesh mesh);
                __instance = mesh;
                return false;
            }
            
            return true;
        }

	}
    */
}

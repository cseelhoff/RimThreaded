using UnityEngine;
using System.Threading;
using UnityEngine.Experimental.Rendering;
using System;
using System.Reflection;

namespace RimThreaded
{

    public class MeshMakerPlanes_Patch
    {

        public static bool NewPlaneMesh(ref Mesh __result, Vector2 size, bool flipped, bool backLift, bool twist)
        {
            int tID = Thread.CurrentThread.ManagedThreadId;
            if (RimThreaded.mainRequestWaits.TryGetValue(tID, out EventWaitHandle eventWaitStart))
            {
                lock (RimThreaded.planeMeshRequests)
                {
                    RimThreaded.planeMeshRequests[tID] = new object[] { size, flipped, backLift, twist };
                }
                RimThreaded.mainThreadWaitHandle.Set();
                eventWaitStart.WaitOne();
                RimThreaded.planeMeshResults.TryGetValue(tID, out Mesh newPlaneMesh);
                __result = newPlaneMesh;
                return false;
            }
            return true;        
        }
    }
}

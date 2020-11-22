using UnityEngine;
using System.Threading;
using UnityEngine.Experimental.Rendering;
using System;
using System.Reflection;
using Verse;

namespace RimThreaded
{

    public class MeshMakerPlanes_Patch
    {
        static readonly Func<object[], object> safeFunction = p =>
            MeshMakerPlanes.NewPlaneMesh((Vector2)p[0], (bool)p[1], (bool)p[2], (bool)p[3]);

        public static bool NewPlaneMesh(ref Mesh __result, Vector2 size, bool flipped, bool backLift, bool twist)
        {
            int tID = Thread.CurrentThread.ManagedThreadId;
            if (RimThreaded.mainRequestWaits.TryGetValue(tID, out EventWaitHandle eventWaitStart))
            {
                object[] functionAndParameters = new object[] { safeFunction, new object[] { size, flipped, backLift, twist } };
                lock (RimThreaded.safeFunctionRequests)
                {
                    RimThreaded.safeFunctionRequests[tID] = functionAndParameters;
                }
                RimThreaded.mainThreadWaitHandle.Set();
                eventWaitStart.WaitOne();
                RimThreaded.safeFunctionResults.TryGetValue(tID, out object safeFunctionResult);
                __result = (Mesh)safeFunctionResult;
                return false;
            }
            return true;
        }
    }
}

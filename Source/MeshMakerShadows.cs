using UnityEngine;
using System.Threading;
using UnityEngine.Experimental.Rendering;
using System;
using System.Reflection;
using Verse;

namespace RimThreaded
{

    public class MeshMakerShadows_Patch
    {
        static readonly Func<object[], object> safeFunction = p =>
            MeshMakerShadows.NewShadowMesh((float)p[0], (float)p[1], (float)p[2]);

        public static bool NewShadowMesh(ref Mesh __result, float baseWidth, float baseHeight, float tallness)
        {
            int tID = Thread.CurrentThread.ManagedThreadId;
            if (RimThreaded.mainRequestWaits.TryGetValue(tID, out EventWaitHandle eventWaitStart))
            {
                object[] functionAndParameters = new object[] { safeFunction, new object[] { baseWidth, baseHeight, tallness } };
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

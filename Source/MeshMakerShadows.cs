using UnityEngine;
using System.Threading;
using System;
using Verse;
using static RimThreaded.RimThreaded;
using static System.Threading.Thread;

namespace RimThreaded
{
    public class MeshMakerShadows_Patch
    {
        static readonly Func<object[], object> safeFunction = parameters =>
            MeshMakerShadows.NewShadowMesh(
                (float)parameters[0], 
                (float)parameters[1], 
                (float)parameters[2]);

        public static bool NewShadowMesh(ref Mesh __result, float baseWidth, float baseHeight, float tallness)
        {
            if (allThreads2.TryGetValue(CurrentThread, out ThreadInfo threadInfo))
            {
                threadInfo.safeFunctionRequest = new object[] { safeFunction, new object[] { baseWidth, baseHeight, tallness } };
                mainThreadWaitHandle.Set();
                threadInfo.eventWaitStart.WaitOne();
                __result = (Mesh)threadInfo.safeFunctionResult;
                return false;
            }
            return true;        
        }
    }
}

﻿using UnityEngine;
using System.Threading;
using System;
using Verse;
using static RimThreaded.RimThreaded;
using static System.Threading.Thread;

namespace RimThreaded
{

    public class MeshMakerPlanes_Patch
    {
        static readonly Func<object[], object> safeFunction = p =>
            MeshMakerPlanes.NewPlaneMesh((Vector2)p[0], (bool)p[1], (bool)p[2], (bool)p[3]);

        public static bool NewPlaneMesh(ref Mesh __result, Vector2 size, bool flipped, bool backLift, bool twist)
        {
            if (allThreads2.TryGetValue(CurrentThread, out ThreadInfo threadInfo))
            {
                threadInfo.safeFunctionRequest = new object[] { safeFunction, new object[] { size, flipped, backLift, twist } };
                mainThreadWaitHandle.Set();
                threadInfo.eventWaitStart.WaitOne();
                __result = (Mesh)threadInfo.safeFunctionResult;
                return false;
            }
            return true;
        }
    }
}

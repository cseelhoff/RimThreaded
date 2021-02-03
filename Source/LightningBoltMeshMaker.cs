﻿using System;
using RimWorld;
using UnityEngine;
using static RimThreaded.RimThreaded;
using static System.Threading.Thread;

namespace RimThreaded
{

    public class LightningBoltMeshMaker_Patch
    {
        static readonly Func<object[], object> safeFunction = parameters =>
            LightningBoltMeshMaker.NewBoltMesh();

        public static bool NewBoltMesh(ref Mesh __result)
        {
            if (allThreads2.TryGetValue(CurrentThread, out ThreadInfo threadInfo))
            {
                threadInfo.safeFunctionRequest = new object[] { safeFunction, new object[] {  } };
                mainThreadWaitHandle.Set();
                threadInfo.eventWaitStart.WaitOne();
                __result = (Mesh)threadInfo.safeFunctionResult;
                return false;
            }
            return true;
        }

    }
}

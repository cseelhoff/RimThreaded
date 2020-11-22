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
        static readonly Func<object[], object> safeFunction = p =>
            LightningBoltMeshMaker.NewBoltMesh();

        public static bool NewBoltMesh(ref Mesh __result)
        {
            int tID = Thread.CurrentThread.ManagedThreadId;
            if (RimThreaded.mainRequestWaits.TryGetValue(tID, out EventWaitHandle eventWaitStart))
            {
                object[] functionAndParameters = new object[] { safeFunction, new object[] {  } };
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

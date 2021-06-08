using System;
using RimWorld;
using UnityEngine;
using static RimThreaded.RimThreaded;
using static System.Threading.Thread;

namespace RimThreaded
{

    public class LightningBoltMeshMaker_Patch
    {
        static readonly Func<object[], object> FuncLightningBoltMeshMaker = parameters =>
            LightningBoltMeshMaker.NewBoltMesh();

        internal static void RunDestructivePatches()
        {
            Type original = typeof(LightningBoltMeshMaker);
            Type patched = typeof(LightningBoltMeshMaker_Patch);
            RimThreadedHarmony.Prefix(original, patched, "NewBoltMesh");
        }

        public static bool NewBoltMesh(ref Mesh __result)
        {
            if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out ThreadInfo threadInfo)) 
                return true;
            threadInfo.safeFunctionRequest = new object[] { FuncLightningBoltMeshMaker, new object[] {  } };
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            __result = (Mesh)threadInfo.safeFunctionResult;
            return false;
        }

    }
}

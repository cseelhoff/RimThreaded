using UnityEngine;
using System;
using Verse;
using static RimThreaded.RimThreaded;
using static System.Threading.Thread;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{

    public class MeshMakerPlanes_Patch
    {
        internal static void RunDestructivePatches()
        {
            Type original = typeof(MeshMakerPlanes);
            Type patched = typeof(MeshMakerPlanes_Patch);
            //RimThreadedHarmony.Prefix(original, patched, "NewPlaneMesh", new Type[] { typeof(Vector2), typeof(bool), typeof(bool), typeof(bool) });
        }

        static readonly Func<object[], object> FuncNewPlaneMesh = p =>
            MeshMakerPlanes.NewPlaneMesh((Vector2)p[0], (bool)p[1], (bool)p[2], (bool)p[3]);



        private static readonly Func<Vector2, bool, bool, bool, Mesh> FuncNewPlaneMesh1 =
            (Func<Vector2, bool, bool, bool, Mesh>)Delegate.CreateDelegate(
                typeof(Func<Vector2, bool, bool, bool, Mesh>),
                Method(typeof(MeshMakerPlanes), "NewPlaneMesh", new Type[] { typeof(Vector2), typeof(bool), typeof(bool), typeof(bool) }));

        private static readonly Func<object[], object> FuncNewPlaneMesh2 = parameters =>
            FuncNewPlaneMesh1((Vector2)parameters[0], (bool)parameters[1], (bool)parameters[2], (bool)parameters[3]);


        public static Mesh NewPlaneMesh(Vector2 size, bool flipped, bool backLift, bool twist)
        {
            if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out ThreadInfo threadInfo)) 
                return MeshMakerPlanes.NewPlaneMesh(size, flipped, backLift, twist);
            threadInfo.safeFunctionRequest = new object[] { FuncNewPlaneMesh2, new object[] { size, flipped, backLift, twist } };
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return (Mesh)threadInfo.safeFunctionResult;
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static RimThreaded.RimThreaded;
using static System.Threading.Thread;

namespace RimThreaded
{
    class GenDraw_Patch
    {

        internal static void RunDestructivePatches()
        {
            Type original = typeof(GenDraw);
            Type patched = typeof(GenDraw_Patch);


            RimThreadedHarmony.Prefix(original, patched, "DrawMeshNowOrLater", new Type[] { typeof(Mesh), typeof(Vector3), typeof(Quaternion), typeof(Material), typeof(bool) });
        }

        private static readonly Action<object[]> ActionDrawMeshNowOrLater = p =>
            GenDraw.DrawMeshNowOrLater((Mesh)p[0], (Vector3)p[1], (Quaternion)p[2], (Material)p[3], (bool)p[4]);




        public static bool DrawMeshNowOrLater(Mesh mesh, Vector3 loc, Quaternion quat, Material mat, bool drawNow)
        {
            if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out ThreadInfo threadInfo))
                return true;

            threadInfo.safeFunctionRequest = new object[] { ActionDrawMeshNowOrLater, new object[] { mesh, loc, quat, mat, drawNow } };
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return false;
        }

    }
}

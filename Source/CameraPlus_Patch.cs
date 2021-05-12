using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static HarmonyLib.AccessTools;
using static RimThreaded.RimThreaded;
using static System.Threading.Thread;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using HarmonyLib;

namespace RimThreaded
{
    class CameraPlus_Patch
    {
        
        public static void RunNonDestructivePatches()
        {
            //Type typeCameraPlus = Type.GetType("CameraPlus");
            Type typeCameraPlus_Tools = Type.GetType("CameraPlus.Tools");
            if (typeCameraPlus_Tools == null) return;
            RimThreadedHarmony.harmony.Patch(Method(typeCameraPlus_Tools, "MouseDistanceSquared", new Type[] { typeof(Vector3), typeof(bool) }), transpiler: RimThreadedHarmony.InputGetMousePositionTranspiler);

        }
    }
}

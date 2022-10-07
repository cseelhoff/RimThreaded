using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace RimThreaded.RW_Patches
{

    public class PawnPath_Patch
    {
        internal static void RunDestructivePatches()
        {
            Type original = typeof(PawnPath);
            Type patched = typeof(PawnPath_Patch);
            RimThreadedHarmony.Prefix(original, patched, "AddNode");
            RimThreadedHarmony.Prefix(original, patched, "ReleaseToPool");
        }
        public static bool AddNode(PawnPath __instance, IntVec3 nodePosition)
        {
            lock (__instance)
            {
                __instance.nodes.Add(nodePosition);
            }
            return false;
        }
        public static bool ReleaseToPool(PawnPath __instance)
        {
            if (__instance == PawnPath.NotFound) return false;
            __instance.totalCostInt = 0f;
            __instance.usedRegionHeuristics = false;
            lock (__instance)
            {
                __instance.nodes = new List<IntVec3>();
            }
            __instance.inUse = false;
            return false;
        }

    }
}

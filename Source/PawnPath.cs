using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace RimThreaded
{

    public class PawnPath_Patch
    {
        public static AccessTools.FieldRef<PawnPath, List<IntVec3>> nodes =
            AccessTools.FieldRefAccess<PawnPath, List<IntVec3>>("nodes");
        public static AccessTools.FieldRef<PawnPath, float> totalCostInt =
            AccessTools.FieldRefAccess<PawnPath, float>("totalCostInt");
        public static AccessTools.FieldRef<PawnPath, bool> usedRegionHeuristics =
            AccessTools.FieldRefAccess<PawnPath, bool>("usedRegionHeuristics");
        public static bool AddNode(PawnPath __instance, IntVec3 nodePosition)
        {
            lock (nodes(__instance))
            {
                nodes(__instance).Add(nodePosition);
            }
            return false;
        }
        public static bool ReleaseToPool(PawnPath __instance)
        {
            if (__instance != PawnPath.NotFound)
            {
                totalCostInt(__instance) = 0f;
                usedRegionHeuristics(__instance) = false;
                lock (nodes(__instance)) {
                    nodes(__instance).Clear();
                }
                __instance.inUse = false;
            }
            return false;
        }

        internal static void RunDestructivePatches()
        {
            Type original = typeof(PawnPath);
            Type patched = typeof(PawnPath_Patch);
            RimThreadedHarmony.Prefix(original, patched, "AddNode");
            RimThreadedHarmony.Prefix(original, patched, "ReleaseToPool");
        }
    }
}

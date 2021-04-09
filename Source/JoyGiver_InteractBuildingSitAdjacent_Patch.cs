using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RimThreaded
{
    class JoyGiver_InteractBuildingSitAdjacent_Patch
    {
        [ThreadStatic] public static List<IntVec3> tmpCells;

        public static void InitializeThreadStatics()
        {
            tmpCells = new List<IntVec3>();
        }

        internal static void RunNonDestructivePatches()
        {
            Type original = typeof(JoyGiver_InteractBuildingSitAdjacent);
            Type patched = typeof(JoyGiver_InteractBuildingSitAdjacent_Patch);
            RimThreadedHarmony.AddAllMatchingFields(original, patched);
            RimThreadedHarmony.TranspileFieldReplacements(original, "TryGivePlayJob");
        }
    }
}
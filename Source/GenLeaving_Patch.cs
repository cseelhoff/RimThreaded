using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RimThreaded
{
    class GenLeaving_Patch
    {
        [ThreadStatic] public static List<IntVec3> tmpCellsCandidates;

        public static void InitializeThreadStatics()
        {
            tmpCellsCandidates = new List<IntVec3>();
        }

        internal static void RunNonDestructivePatches()
        {
            Type original = typeof(GenLeaving);
            Type patched = typeof(GenLeaving_Patch);
            RimThreadedHarmony.AddAllMatchingFields(original, patched);
            RimThreadedHarmony.TranspileFieldReplacements(original, "DropFilthDueToDamage");
        }
    }
}

using System.Collections.Generic;
using Verse;
using System;

namespace RimThreaded
{
    public class Verb_Patch
    {
        [ThreadStatic] public static List<IntVec3> tempLeanShootSources = new List<IntVec3>();
        [ThreadStatic] public static List<IntVec3> tempDestList = new List<IntVec3>();

        public static void InitializeThreadStatics()
        {
            tempLeanShootSources = new List<IntVec3>();
            tempDestList = new List<IntVec3>();
        }
        internal static void RunNonDestructivePatches()
        {
            Type original = typeof(Verb);
            Type patched = typeof(Verb_Patch);
            RimThreadedHarmony.AddAllMatchingFields(original, patched);
            RimThreadedHarmony.TranspileFieldReplacements(original, "TryFindShootLineFromTo");
            RimThreadedHarmony.TranspileFieldReplacements(original, "CanHitFromCellIgnoringRange");
        }


    }
}

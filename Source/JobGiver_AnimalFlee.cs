using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RimThreaded
{

    public class JobGiver_AnimalFlee_Patch
    {
        [ThreadStatic] public static List<Thing> tmpThings;

        public static void InitializeThreadStatics()
        {
            tmpThings = new List<Thing>();
        }
        internal static void RunNonDestructivePatches()
        {
            Type original = typeof(JobGiver_AnimalFlee);
            Type patched = typeof(JobGiver_AnimalFlee_Patch);
            RimThreadedHarmony.AddAllMatchingFields(original, patched);
            RimThreadedHarmony.TranspileFieldReplacements(original, "FleeJob");
        }
    }
}

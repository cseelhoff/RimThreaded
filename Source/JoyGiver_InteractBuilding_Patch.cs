using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimThreaded
{
    class JoyGiver_InteractBuilding_Patch
    {
        [ThreadStatic] public static List<Thing> tmpCandidates;

        public static void InitializeThreadStatics()
        {
            tmpCandidates = new List<Thing>();
        }

        internal static void RunNonDestructivePatches()
        {
            Type original = typeof(JoyGiver_InteractBuilding);
            Type patched = typeof(JoyGiver_InteractBuilding_Patch);
            RimThreadedHarmony.AddAllMatchingFields(original, patched);
            RimThreadedHarmony.TranspileFieldReplacements(original, "FindBestGame");
        }
    }
}
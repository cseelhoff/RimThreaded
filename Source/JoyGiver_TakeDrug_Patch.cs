using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RimThreaded
{
    class JoyGiver_TakeDrug_Patch
    {
        [ThreadStatic] public static List<ThingDef> takeableDrugs;

        public static void InitializeThreadStatics()
        {
            takeableDrugs = new List<ThingDef>();
        }

        internal static void RunNonDestructivePatches()
        {
            Type original = typeof(JoyGiver_TakeDrug);
            Type patched = typeof(JoyGiver_TakeDrug_Patch);
            RimThreadedHarmony.AddAllMatchingFields(original, patched);
            RimThreadedHarmony.TranspileFieldReplacements(original, "BestIngestItem");
        }
    }
}
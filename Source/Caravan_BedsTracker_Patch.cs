using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Planet;

namespace RimThreaded
{
    class Caravan_BedsTracker_Patch
    {
        [ThreadStatic] public static List<Building_Bed> tmpUsableBeds;
        [ThreadStatic] public static List<string> tmpPawnLabels;
        public static void InitializeThreadStatics()
        {
            tmpUsableBeds = new List<Building_Bed>();
            tmpPawnLabels = new List<string>();
        }

        internal static void RunNonDestructivePatches()
        {
            Type original = typeof(Caravan_BedsTracker);
            Type patched = typeof(Caravan_BedsTracker_Patch);
            RimThreadedHarmony.AddAllMatchingFields(original, patched);
            RimThreadedHarmony.TranspileFieldReplacements(original, "RecalculateUsedBeds");
            RimThreadedHarmony.TranspileFieldReplacements(original, "GetInBedForMedicalReasonsInspectStringLine");
        }
    }
}

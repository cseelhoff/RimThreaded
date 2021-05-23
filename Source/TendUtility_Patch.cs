using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RimThreaded
{

    public class TendUtility_Patch
    {
        [ThreadStatic] public static List<Hediff> tmpHediffsToTend;
        [ThreadStatic] public static List<Hediff> tmpHediffs;
        [ThreadStatic] public static List<Pair<Hediff, float>> tmpHediffsWithTendPriority;

        internal static void InitializeThreadStatics()
        {
            tmpHediffsToTend = new List<Hediff>();
            tmpHediffs = new List<Hediff>();
            tmpHediffsWithTendPriority = new List<Pair<Hediff, float>>();
        }

        internal static void RunNonDestructivePatches()
        {
            Type original = typeof(TendUtility);
            Type patched = typeof(TendUtility_Patch);
            RimThreadedHarmony.AddAllMatchingFields(original, patched);
            RimThreadedHarmony.TranspileFieldReplacements(original, "DoTend");
            RimThreadedHarmony.TranspileFieldReplacements(original, "GetOptimalHediffsToTendWithSingleTreatment");
            RimThreadedHarmony.TranspileFieldReplacements(original, "SortByTendPriority");
        }


    }
}

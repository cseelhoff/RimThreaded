using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RimThreaded
{

    public class Medicine_Patch
    {
        [ThreadStatic] public static List<Hediff> tendableHediffsInTendPriorityOrder;
        [ThreadStatic] public static List<Hediff> tmpHediffs;

        public static void InitializeThreadStatics()
        {
            tendableHediffsInTendPriorityOrder = new List<Hediff>();
            tmpHediffs = new List<Hediff>();
        }

        internal static void RunNonDestructivePatches()
        {
            Type original = typeof(Medicine);
            Type patched = typeof(Medicine_Patch);
            RimThreadedHarmony.AddAllMatchingFields(original, patched);
            RimThreadedHarmony.TranspileFieldReplacements(original, "GetMedicineCountToFullyHeal");
        }



    }
}

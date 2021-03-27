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

    public class Medicine_Patch
    {
        public static bool GetMedicineCountToFullyHeal(ref int __result, Pawn pawn)
        {
            int num = 0;
            int num2 = pawn.health.hediffSet.hediffs.Count + 1;
            List<Hediff> tendableHediffsInTendPriorityOrder = new List<Hediff>();
            List<Hediff> tmpHediffs = new List<Hediff>();
            //tendableHediffsInTendPriorityOrder.Clear();
            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                if (hediffs[i].TendableNow())
                {
                    tendableHediffsInTendPriorityOrder.Add(hediffs[i]);
                }
            }

            TendUtility.SortByTendPriority(tendableHediffsInTendPriorityOrder);
            int num3 = 0;
            while (true)
            {
                num++;
                if (num > num2)
                {
                    Log.Error("Too many iterations.");
                    break;
                }

                TendUtility.GetOptimalHediffsToTendWithSingleTreatment(pawn, usingMedicine: true, tmpHediffs, tendableHediffsInTendPriorityOrder);
                if (!tmpHediffs.Any())
                {
                    break;
                }

                num3++;
                for (int j = 0; j < tmpHediffs.Count; j++)
                {
                    tendableHediffsInTendPriorityOrder.Remove(tmpHediffs[j]);
                }
            }

            //tmpHediffs.Clear();
            //tendableHediffsInTendPriorityOrder.Clear();
            __result = num3;
            return false;
        }

        internal static void RunDestructivePatches()
        {
            Type original = typeof(Medicine);
            Type patched = typeof(Medicine_Patch);
            RimThreadedHarmony.Prefix(original, patched, "GetMedicineCountToFullyHeal");
        }
    }
}

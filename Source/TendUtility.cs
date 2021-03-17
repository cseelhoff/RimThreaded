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

    public class TendUtility_Patch
	{
		public static bool GetOptimalHediffsToTendWithSingleTreatment(
            Pawn patient,
            bool usingMedicine,
            List<Hediff> outHediffsToTend,
            List<Hediff> tendableHediffsInTendPriorityOrder = null)
        {
            outHediffsToTend.Clear();
            //TendUtility.tmpHediffs.Clear();
            List<Hediff> tmpHediffs = new List<Hediff>();
            if (tendableHediffsInTendPriorityOrder != null)
            {
                tmpHediffs.AddRange((IEnumerable<Hediff>)tendableHediffsInTendPriorityOrder);
            }
            else
            {
                List<Hediff> hediffs = patient.health.hediffSet.hediffs;
                for (int index = 0; index < hediffs.Count; ++index)
                {
                    if (hediffs[index].TendableNow(false))
                        tmpHediffs.Add(hediffs[index]);
                }
                TendUtility.SortByTendPriority(tmpHediffs);
            }
            if (!tmpHediffs.Any())
                return false;
            Hediff tmpHediff1 = tmpHediffs[0];
            outHediffsToTend.Add(tmpHediff1);
            HediffCompProperties_TendDuration propertiesTendDuration = tmpHediff1.def.CompProps<HediffCompProperties_TendDuration>();
            if (propertiesTendDuration != null && propertiesTendDuration.tendAllAtOnce)
            {
                for (int index = 0; index < tmpHediffs.Count; ++index)
                {
                    if (tmpHediffs[index] != tmpHediff1 && tmpHediffs[index].def == tmpHediff1.def)
                        outHediffsToTend.Add(tmpHediffs[index]);
                }
            }
            else if (tmpHediff1 is Hediff_Injury & usingMedicine)
            {
                float severity1 = tmpHediff1.Severity;
                for (int index = 0; index < tmpHediffs.Count; ++index)
                {
                    if (tmpHediffs[index] != tmpHediff1 && tmpHediffs[index] is Hediff_Injury tmpHediff2)
                    {
                        float severity2 = tmpHediff2.Severity;
                        if ((double)severity1 + (double)severity2 <= 20.0)
                        {
                            severity1 += severity2;
                            outHediffsToTend.Add((Hediff)tmpHediff2);
                        }
                    }
                }
            }
            //TendUtility.tmpHediffs.Clear();
            return false;
        }

    }
}

using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using static Verse.PawnCapacityUtility;
using UnityEngine;

namespace RimThreaded
{

    public class PawnCapacityUtility_Patch
	{
        public static bool CalculatePartEfficiency(ref float __result, HediffSet diffSet, BodyPartRecord part, bool ignoreAddedParts = false, List<CapacityImpactor> impactors = null)
        {
            BodyPartRecord rec;
            for (rec = part.parent; rec != null; rec = rec.parent)
            {
                if (diffSet.HasDirectlyAddedPartFor(rec))
                {
                    Hediff_AddedPart hediff_AddedPart = (from x in diffSet.GetHediffs<Hediff_AddedPart>()
                                                         where x.Part == rec
                                                         select x).First();
                    impactors?.Add(new CapacityImpactorHediff
                    {
                        hediff = hediff_AddedPart
                    });
                    __result = hediff_AddedPart.def.addedPartProps.partEfficiency;
                    return false;
                }
            }

            if (part.parent != null && diffSet.PartIsMissing(part.parent))
            {
                __result = 0f;
                return false;
            }

            float num = 1f;
            if (!ignoreAddedParts)
            {
                for (int i = 0; i < diffSet.hediffs.Count; i++)
                {
                    Hediff_AddedPart hediff_AddedPart2 = diffSet.hediffs[i] as Hediff_AddedPart;
                    if (hediff_AddedPart2 != null && hediff_AddedPart2.Part == part)
                    {
                        num *= hediff_AddedPart2.def.addedPartProps.partEfficiency;
                        if (hediff_AddedPart2.def.addedPartProps.partEfficiency != 1f)
                        {
                            impactors?.Add(new CapacityImpactorHediff
                            {
                                hediff = hediff_AddedPart2
                            });
                        }
                    }
                }
            }

            float b = -1f;
            float num2 = 0f;
            bool flag = false;
            for (int j = 0; j < diffSet.hediffs.Count; j++)
            {
                Hediff hediff1 = diffSet.hediffs[j];
                if (hediff1 != null && hediff1.Part == part && hediff1.CurStage != null)
                {
                    HediffStage curStage = hediff1.CurStage;
                    num2 += curStage.partEfficiencyOffset;
                    flag |= curStage.partIgnoreMissingHP;
                    if (curStage.partEfficiencyOffset != 0f && curStage.becomeVisible)
                    {
                        impactors?.Add(new CapacityImpactorHediff
                        {
                            hediff = hediff1
                        });
                    }
                }
            }

            if (!flag)
            {
                float num3 = diffSet.GetPartHealth(part) / part.def.GetMaxHealth(diffSet.pawn);
                if (num3 != 1f)
                {
                    if (DamageWorker_AddInjury.ShouldReduceDamageToPreservePart(part))
                    {
                        num3 = Mathf.InverseLerp(0.1f, 1f, num3);
                    }

                    impactors?.Add(new CapacityImpactorBodyPartHealth
                    {
                        bodyPart = part
                    });
                    num *= num3;
                }
            }

            num += num2;
            if (num > 0.0001f)
            {
                num = Mathf.Max(num, b);
            }

            __result = Mathf.Max(num, 0f);
            return false;
        }



    }
}

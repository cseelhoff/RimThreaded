using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimThreaded
{
    class StatWorker_Patch
    {
        public static bool GetValueUnfinalized(StatWorker __instance, ref float __result, StatRequest req, bool applyPostProcess = true)
        {
            /*
            if (!stat.supressDisabledError && Prefs.DevMode && IsDisabledFor(req.Thing))
            {
                Log.ErrorOnce($"Attempted to calculate value for disabled stat {stat}; this is meant as a consistency check, either set the stat to neverDisabled or ensure this pawn cannot accidentally use this stat (thing={req.Thing.ToStringSafe()})", 75193282 + stat.index);
            }

            float num = GetBaseValueFor(req);
            Pawn pawn = req.Thing as Pawn;
            if (pawn != null)
            {
                if (pawn.skills != null)
                {
                    if (stat.skillNeedOffsets != null)
                    {
                        for (int i = 0; i < stat.skillNeedOffsets.Count; i++)
                        {
                            num += stat.skillNeedOffsets[i].ValueFor(pawn);
                        }
                    }
                }
                else
                {
                    num += stat.noSkillOffset;
                }

                if (stat.capacityOffsets != null)
                {
                    for (int j = 0; j < stat.capacityOffsets.Count; j++)
                    {
                        PawnCapacityOffset pawnCapacityOffset = stat.capacityOffsets[j];
                        num += pawnCapacityOffset.GetOffset(pawn.health.capacities.GetLevel(pawnCapacityOffset.capacity));
                    }
                }

                if (pawn.story != null)
                {
                    for (int k = 0; k < pawn.story.traits.allTraits.Count; k++)
                    {
                        num += pawn.story.traits.allTraits[k].OffsetOfStat(stat);
                    }
                }

                List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
                for (int l = 0; l < hediffs.Count; l++)
                {
                    HediffStage curStage = hediffs[l].CurStage;
                    if (curStage != null)
                    {
                        float num2 = curStage.statOffsets.GetStatOffsetFromList(stat);
                        if (num2 != 0f && curStage.statOffsetEffectMultiplier != null)
                        {
                            num2 *= pawn.GetStatValue(curStage.statOffsetEffectMultiplier);
                        }

                        num += num2;
                    }
                }

                if (pawn.apparel != null)
                {
                    for (int m = 0; m < pawn.apparel.WornApparel.Count; m++)
                    {
                        num += StatOffsetFromGear(pawn.apparel.WornApparel[m], stat);
                    }
                }

                if (pawn.equipment != null && pawn.equipment.Primary != null)
                {
                    num += StatOffsetFromGear(pawn.equipment.Primary, stat);
                }

                if (pawn.story != null)
                {
                    for (int n = 0; n < pawn.story.traits.allTraits.Count; n++)
                    {
                        num *= pawn.story.traits.allTraits[n].MultiplierOfStat(stat);
                    }
                }

                for (int num3 = 0; num3 < hediffs.Count; num3++)
                {
                    HediffStage curStage2 = hediffs[num3].CurStage;
                    if (curStage2 != null)
                    {
                        float num4 = curStage2.statFactors.GetStatFactorFromList(stat);
                        if (Math.Abs(num4 - 1f) > float.Epsilon && curStage2.statFactorEffectMultiplier != null)
                        {
                            num4 = ScaleFactor(num4, pawn.GetStatValue(curStage2.statFactorEffectMultiplier));
                        }

                        num *= num4;
                    }
                }

                num *= pawn.ageTracker.CurLifeStage.statFactors.GetStatFactorFromList(stat);
            }

            if (req.StuffDef != null)
            {
                if (num > 0f || stat.applyFactorsIfNegative)
                {
                    num *= req.StuffDef.stuffProps.statFactors.GetStatFactorFromList(stat);
                }

                num += req.StuffDef.stuffProps.statOffsets.GetStatOffsetFromList(stat);
            }

            if (req.ForAbility && stat.statFactors != null)
            {
                for (int num5 = 0; num5 < stat.statFactors.Count; num5++)
                {
                    num *= req.AbilityDef.statBases.GetStatValueFromList(stat.statFactors[num5], 1f);
                }
            }

            if (req.HasThing)
            {
                CompAffectedByFacilities compAffectedByFacilities = req.Thing.TryGetComp<CompAffectedByFacilities>();
                if (compAffectedByFacilities != null)
                {
                    num += compAffectedByFacilities.GetStatOffset(stat);
                }

                if (stat.statFactors != null)
                {
                    for (int num6 = 0; num6 < stat.statFactors.Count; num6++)
                    {
                        num *= req.Thing.GetStatValue(stat.statFactors[num6]);
                    }
                }

                if (pawn != null)
                {
                    if (pawn.skills != null)
                    {
                        if (stat.skillNeedFactors != null)
                        {
                            for (int num7 = 0; num7 < stat.skillNeedFactors.Count; num7++)
                            {
                                num *= stat.skillNeedFactors[num7].ValueFor(pawn);
                            }
                        }
                    }
                    else
                    {
                        num *= stat.noSkillFactor;
                    }

                    if (stat.capacityFactors != null)
                    {
                        for (int num8 = 0; num8 < stat.capacityFactors.Count; num8++)
                        {
                            PawnCapacityFactor pawnCapacityFactor = stat.capacityFactors[num8];
                            float factor = pawnCapacityFactor.GetFactor(pawn.health.capacities.GetLevel(pawnCapacityFactor.capacity));
                            num = Mathf.Lerp(num, num * factor, pawnCapacityFactor.weight);
                        }
                    }

                    if (pawn.Inspired)
                    {
                        num += pawn.InspirationDef.statOffsets.GetStatOffsetFromList(stat);
                        num *= pawn.InspirationDef.statFactors.GetStatFactorFromList(stat);
                    }
                }
            }

            return num;
            */
            return false;
        }


    }
}

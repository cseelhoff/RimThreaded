using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace RimThreaded
{

    public class Pawn_HealthTracker_Patch
    {
        internal static void RunDestructivePatches()
        {
            Type original = typeof(Pawn_HealthTracker);
            Type patched = typeof(Pawn_HealthTracker_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(RemoveHediff));
            RimThreadedHarmony.Prefix(original, patched, nameof(RestorePartRecursiveInt));
            RimThreadedHarmony.Prefix(original, patched, nameof(CheckPredicateAfterAddingHediff));
            RimThreadedHarmony.Prefix(original, patched, nameof(Notify_Resurrected));
#if RW13
            RimThreadedHarmony.Prefix(original, patched, nameof(HealthTick));
#endif
            RimThreadedHarmony.Prefix(original, patched, nameof(SetDead)); //optional warning instead of error
        }
        public static bool SetDead(Pawn_HealthTracker __instance)
        {
            if (__instance.Dead)
                Log.Warning(__instance.pawn.ToString() + " set dead while already dead."); //changed
            __instance.healthState = PawnHealthState.Dead;
            return false;
        }

        public static bool RemoveHediff(Pawn_HealthTracker __instance, Hediff hediff)
        {
            if (__instance.hediffSet == null || __instance.hediffSet.hediffs == null)
                return false;

            lock (__instance.hediffSet)
            {
                List<Hediff> newHediffs = new List<Hediff>(__instance.hediffSet.hediffs);
                newHediffs.Remove(hediff);
                __instance.hediffSet.hediffs = newHediffs;
            }
            hediff.PostRemoved();
            __instance.Notify_HediffChanged(null);

            return false;
        }
        public static bool RestorePartRecursiveInt(Pawn_HealthTracker __instance, BodyPartRecord part, Hediff diffException = null)
        {
            lock (__instance.hediffSet)
            {
                List<Hediff> newHediffs = new List<Hediff>(__instance.hediffSet.hediffs); //added
                //List<Hediff> hediffs = __instance.hediffSet.hediffs; //removed
                for (int index = newHediffs.Count - 1; index >= 0; --index)
                {
                    Hediff hediff = newHediffs[index];
                    if (hediff.Part == part && hediff != diffException && !hediff.def.keepOnBodyPartRestoration)
                    {
                        newHediffs.RemoveAt(index);
                        __instance.hediffSet.hediffs = newHediffs; //added
                        hediff.PostRemoved();
                    }
                }
                for (int index = 0; index < part.parts.Count; ++index)
                    __instance.RestorePartRecursiveInt(part.parts[index], diffException);
            }
            return false;
        }
        public static bool CheckPredicateAfterAddingHediff(Pawn_HealthTracker __instance, ref bool __result, Hediff hediff, Func<bool> pred)
        {
            lock (__instance.hediffSet) //added
            {
                List<Hediff> newHediffs = new List<Hediff>(__instance.hediffSet.hediffs); //added
                HashSet<Hediff> missing = __instance.CalculateMissingPartHediffsFromInjury(hediff);
                newHediffs.Add(hediff);
                if (missing != null)
                    newHediffs.AddRange(missing);
                __instance.hediffSet.hediffs = newHediffs;//added
                __instance.hediffSet.DirtyCache();
                int num = pred() ? 1 : 0;
                if (missing != null)
                    newHediffs.RemoveAll(x => missing.Contains(x));
                newHediffs.Remove(hediff);
                __instance.hediffSet.hediffs = newHediffs;//added
                __instance.hediffSet.DirtyCache();
                __result = num != 0;
            }
            return false;
        }
        public static bool Notify_Resurrected(Pawn_HealthTracker __instance)
        {
            lock (__instance.hediffSet) //added
            {
                List<Hediff> newHediffs = new List<Hediff>(__instance.hediffSet.hediffs); //added
                __instance.healthState = PawnHealthState.Mobile;
                newHediffs.RemoveAll((Predicate<Hediff>)(x => x.def.everCurableByItem && x.TryGetComp<HediffComp_Immunizable>() != null));
                newHediffs.RemoveAll((Predicate<Hediff>)(x => x.def.everCurableByItem && x is Hediff_Injury && !x.IsPermanent()));
                newHediffs.RemoveAll((Predicate<Hediff>)(x =>
                {
                    if (!x.def.everCurableByItem)
                        return false;
                    if ((double)x.def.lethalSeverity >= 0.0)
                        return true;
                    return x.def.stages != null && x.def.stages.Any<HediffStage>((Predicate<HediffStage>)(y => y.lifeThreatening));
                }));
                newHediffs.RemoveAll((Predicate<Hediff>)(x => x.def.everCurableByItem && x is Hediff_Injury && x.IsPermanent() && (double)__instance.hediffSet.GetPartHealth(x.Part) <= 0.0));
                __instance.hediffSet.hediffs = newHediffs; //added
                while (true)
                {
                    Hediff_MissingPart hediffMissingPart = __instance.hediffSet.GetMissingPartsCommonAncestors().Where<Hediff_MissingPart>((Func<Hediff_MissingPart, bool>)(x => !__instance.hediffSet.PartOrAnyAncestorHasDirectlyAddedParts(x.Part))).FirstOrDefault<Hediff_MissingPart>();
                    if (hediffMissingPart != null)
                        __instance.RestorePart(hediffMissingPart.Part, checkStateChange: false);
                    else
                        break;
                }
                __instance.hediffSet.DirtyCache();
                if (__instance.ShouldBeDead())
                    __instance.hediffSet.hediffs.RemoveAll((Predicate<Hediff>)(h => !h.def.keepOnBodyPartRestoration));
                __instance.Notify_HediffChanged((Hediff)null);
            }
            return false;
        }

#if RW13
        public static bool HealthTick(Pawn_HealthTracker __instance)
        {
            if (__instance.Dead)
                return false;
            for (int index = __instance.hediffSet.hediffs.Count - 1; index >= 0; --index)
            {
                Hediff hediff = __instance.hediffSet.hediffs[index];
                try
                {
                    hediff.Tick();
                    hediff.PostTick();
                }
                catch (Exception ex1)
                {
                    Log.Error("Exception ticking hediff " + hediff.ToStringSafe<Hediff>() + " for pawn " + __instance.pawn.ToStringSafe<Pawn>() + ". Removing hediff... Exception: " + (object)ex1);
                    try
                    {
                        __instance.RemoveHediff(hediff);
                    }
                    catch (Exception ex2)
                    {
                        Log.Error("Error while removing hediff: " + (object)ex2);
                    }
                }
                if (__instance.Dead)
                    return false;
            }
            bool flag1 = false;
            lock (__instance.hediffSet) //added
            {
                List<Hediff> newHediffs = new List<Hediff>(__instance.hediffSet.hediffs); //added
                for (int index = newHediffs.Count - 1; index >= 0; --index) //changed
                {
                    Hediff hediff = newHediffs[index];
                    if (hediff.ShouldRemove)
                    {
                        newHediffs.RemoveAt(index); //changed
                        __instance.hediffSet.hediffs = newHediffs; //added
                        hediff.PostRemoved();
                        flag1 = true;
                    }
                }
            }
            if (flag1)
                __instance.Notify_HediffChanged((Hediff)null);
            if (__instance.Dead)
                return false;
            __instance.immunity.ImmunityHandlerTick();
            if (__instance.pawn.RaceProps.IsFlesh && __instance.pawn.IsHashIntervalTick(600) && (__instance.pawn.needs.food == null || !__instance.pawn.needs.food.Starving))
            {
                bool flag2 = false;
                if (__instance.hediffSet.HasNaturallyHealingInjury())
                {
                    float num = 8f;
                    if (__instance.pawn.GetPosture() != PawnPosture.Standing)
                    {
                        num += 4f;
                        Building_Bed buildingBed = __instance.pawn.CurrentBed();
                        if (buildingBed != null)
                            num += buildingBed.def.building.bed_healPerDay;
                    }
                    foreach (Hediff hediff in __instance.hediffSet.hediffs)
                    {
                        HediffStage curStage = hediff.CurStage;
                        if (curStage != null && (double)curStage.naturalHealingFactor != -1.0)
                            num *= curStage.naturalHealingFactor;
                    }
                    __instance.hediffSet.GetHediffs<Hediff_Injury>().Where<Hediff_Injury>((Func<Hediff_Injury, bool>)(x => x.CanHealNaturally())).RandomElement<Hediff_Injury>().Heal((float)((double)num * (double)__instance.pawn.HealthScale * 0.00999999977648258) * __instance.pawn.GetStatValue(StatDefOf.InjuryHealingFactor));
                    flag2 = true;
                }
                if (__instance.hediffSet.HasTendedAndHealingInjury() && (__instance.pawn.needs.food == null || !__instance.pawn.needs.food.Starving))
                {
                    Hediff_Injury hd = __instance.hediffSet.GetHediffs<Hediff_Injury>().Where<Hediff_Injury>((Func<Hediff_Injury, bool>)(x => x.CanHealFromTending())).RandomElement<Hediff_Injury>();
                    hd.Heal((float)(8.0 * (double)GenMath.LerpDouble(0.0f, 1f, 0.5f, 1.5f, Mathf.Clamp01(hd.TryGetComp<HediffComp_TendDuration>().tendQuality)) * (double)__instance.pawn.HealthScale * 0.00999999977648258) * __instance.pawn.GetStatValue(StatDefOf.InjuryHealingFactor));
                    flag2 = true;
                }
                if (flag2 && !__instance.HasHediffsNeedingTendByPlayer() && (!HealthAIUtility.ShouldSeekMedicalRest(__instance.pawn) && !__instance.hediffSet.HasTendedAndHealingInjury()) && PawnUtility.ShouldSendNotificationAbout(__instance.pawn))
                    Messages.Message((string)"MessageFullyHealed".Translate((NamedArgument)__instance.pawn.LabelCap, (NamedArgument)(Thing)__instance.pawn), (LookTargets)(Thing)__instance.pawn, MessageTypeDefOf.PositiveEvent);
            }
            if (__instance.pawn.RaceProps.IsFlesh && (double)__instance.hediffSet.BleedRateTotal >= 0.100000001490116)
            {
                float num = __instance.hediffSet.BleedRateTotal * __instance.pawn.BodySize;
                if ((double)Rand.Value < (__instance.pawn.GetPosture() != PawnPosture.Standing ? (double)(num * 0.0004f) : (double)(num * 0.004f)))
                    __instance.DropBloodFilth();
            }
            if (!__instance.pawn.IsHashIntervalTick(60))
                return false;
            List<HediffGiverSetDef> hediffGiverSets = __instance.pawn.RaceProps.hediffGiverSets;
            if (hediffGiverSets != null)
            {
                for (int index1 = 0; index1 < hediffGiverSets.Count; ++index1)
                {
                    List<HediffGiver> hediffGivers = hediffGiverSets[index1].hediffGivers;
                    for (int index2 = 0; index2 < hediffGivers.Count; ++index2)
                    {
                        hediffGivers[index2].OnIntervalPassed(__instance.pawn, (Hediff)null);
                        if (__instance.pawn.Dead)
                            return false;
                    }
                }
            }
            if (__instance.pawn.story == null)
                return false;
            List<Trait> allTraits = __instance.pawn.story.traits.allTraits;
            for (int index = 0; index < allTraits.Count; ++index)
            {
                TraitDegreeData currentData = allTraits[index].CurrentData;
                if ((double)currentData.randomDiseaseMtbDays > 0.0 && Rand.MTBEventOccurs(currentData.randomDiseaseMtbDays, 60000f, 60f))
                {
                    BiomeDef biome = __instance.pawn.Tile == -1 ? DefDatabase<BiomeDef>.GetRandom() : Find.WorldGrid[__instance.pawn.Tile].biome;
                    IncidentDef incidentDef = DefDatabase<IncidentDef>.AllDefs.Where<IncidentDef>((Func<IncidentDef, bool>)(d => d.category == IncidentCategoryDefOf.DiseaseHuman)).RandomElementByWeightWithFallback<IncidentDef>((Func<IncidentDef, float>)(d => biome.CommonalityOfDisease(d)));
                    if (incidentDef != null)
                    {
                        string blockedInfo;
                        List<Pawn> pawns = ((IncidentWorker_Disease)incidentDef.Worker).ApplyToPawns(Gen.YieldSingle<Pawn>(__instance.pawn), out blockedInfo);
                        if (PawnUtility.ShouldSendNotificationAbout(__instance.pawn))
                        {
                            if (pawns.Contains(__instance.pawn))
                                Find.LetterStack.ReceiveLetter("LetterLabelTraitDisease".Translate((NamedArgument)incidentDef.diseaseIncident.label), "LetterTraitDisease".Translate((NamedArgument)__instance.pawn.LabelCap, (NamedArgument)incidentDef.diseaseIncident.label, __instance.pawn.Named("PAWN")).AdjustedFor(__instance.pawn), LetterDefOf.NegativeEvent, (LookTargets)(Thing)__instance.pawn);
                            else if (!blockedInfo.NullOrEmpty())
                                Messages.Message(blockedInfo, (LookTargets)(Thing)__instance.pawn, MessageTypeDefOf.NeutralEvent);
                        }
                    }
                }
            }
            return false;
        }
#endif
    }
}

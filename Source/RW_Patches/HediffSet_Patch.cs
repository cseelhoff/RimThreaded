using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace RimThreaded.RW_Patches
{

    public class HediffSet_Patch
    {
        static Type original = typeof(HediffSet);
        static Type patched = typeof(HediffSet_Patch);

        internal static void RunDestructivePatches()
        {
            RimThreadedHarmony.Prefix(original, patched, nameof(AddDirect));
            RimThreadedHarmony.Prefix(original, patched, nameof(Clear));
            RimThreadedHarmony.Prefix(original, patched, nameof(CacheMissingPartsCommonAncestors));
            RimThreadedHarmony.Prefix(original, patched, nameof(HasDirectlyAddedPartFor));
            RimThreadedHarmony.Prefix(original, patched, nameof(PartIsMissing));
            RimThreadedHarmony.Prefix(original, patched, nameof(GetCoverageOfNotMissingNaturalParts));
        }
        internal static void RunNonDestructivePatches()
        {
            RimThreadedHarmony.Postfix(original, patched, nameof(HediffSet.DirtyCache), nameof(DirtyCacheSetInvisbility));
        }
        public static bool GetCoverageOfNotMissingNaturalParts(HediffSet __instance, ref float __result, BodyPartRecord part)
        {
            float num = 0f;
            lock (__instance)
            {
                if (__instance.PartIsMissing(part))
                {
                    __result = 0f;
                    return false;
                }
                if (__instance.PartOrAnyAncestorHasDirectlyAddedParts(part))
                {
                    __result = 0f;
                    return false;
                }
                __instance.coverageRejectedPartsSet.Clear();
                List<Hediff_MissingPart> missingPartsCommonAncestors = __instance.GetMissingPartsCommonAncestors();
                for (int i = 0; i < missingPartsCommonAncestors.Count; i++)
                {
                    __instance.coverageRejectedPartsSet.Add(missingPartsCommonAncestors[i].Part);
                }
                for (int j = 0; j < __instance.hediffs.Count; j++)
                {
                    if (__instance.hediffs[j] is Hediff_AddedPart)
                    {
                        __instance.coverageRejectedPartsSet.Add(__instance.hediffs[j].Part);
                    }
                }
                //float num = 0f; //moved to top
                __instance.coveragePartsStack.Clear();
                __instance.coveragePartsStack.Push(part);
                while (__instance.coveragePartsStack.Any())
                {
                    BodyPartRecord bodyPartRecord = __instance.coveragePartsStack.Pop();
                    num += bodyPartRecord.coverageAbs;
                    for (int k = 0; k < bodyPartRecord.parts.Count; k++)
                    {
                        if (!__instance.coverageRejectedPartsSet.Contains(bodyPartRecord.parts[k]))
                        {
                            __instance.coveragePartsStack.Push(bodyPartRecord.parts[k]);
                        }
                    }
                }
                __instance.coveragePartsStack.Clear();
                __instance.coverageRejectedPartsSet.Clear();
            }
            __result = num;
            return false;
        }

        public static bool PartIsMissing(HediffSet __instance, ref bool __result, BodyPartRecord part)
        {
            List<Hediff> hediffs = __instance.hediffs;
            for (int index = 0; index < hediffs.Count; ++index)
            {
                Hediff hediff = hediffs[index];
                if (hediff != null && hediff.Part == part && hediff is Hediff_MissingPart)
                {
                    __result = true;
                    return false;
                }
            }
            __result = false;
            return false;
        }

        public static bool HasDirectlyAddedPartFor(HediffSet __instance, ref bool __result, BodyPartRecord part)
        {
            List<Hediff> hediffs = __instance.hediffs;
            for (int index = 0; index < hediffs.Count; ++index)
            {
                Hediff hediff = hediffs[index];
                if (hediff != null && hediff.Part == part && hediff is Hediff_AddedPart)
                {
                    __result = true;
                    return false;
                }
            }
            __result = false;
            return false;
        }

        public static bool CacheMissingPartsCommonAncestors(HediffSet __instance)
        {
            lock (__instance) //added
            {
                List<Hediff_MissingPart> cachedMissingPartsCommonAncestors = __instance.cachedMissingPartsCommonAncestors;
                if (cachedMissingPartsCommonAncestors == null)
                    cachedMissingPartsCommonAncestors = new List<Hediff_MissingPart>();
                else
                    __instance.cachedMissingPartsCommonAncestors.Clear();
                Queue<BodyPartRecord> missingPartsCommonAncestorsQueue = __instance.missingPartsCommonAncestorsQueue;
                missingPartsCommonAncestorsQueue.Clear();
                missingPartsCommonAncestorsQueue.Enqueue(__instance.pawn.def.race.body.corePart);
                while (__instance.missingPartsCommonAncestorsQueue.Count != 0)
                {
                    BodyPartRecord bodyPartRecord = __instance.missingPartsCommonAncestorsQueue.Dequeue();
                    if (__instance.PartOrAnyAncestorHasDirectlyAddedParts(bodyPartRecord))
                    {
                        continue;
                    }
                    Hediff_MissingPart firstHediffMatchingPart = __instance.GetFirstHediffMatchingPart<Hediff_MissingPart>(bodyPartRecord);
                    if (firstHediffMatchingPart != null)
                    {
                        cachedMissingPartsCommonAncestors.Add(firstHediffMatchingPart);
                        continue;
                    }
                    for (int index = 0; index < bodyPartRecord.parts.Count; ++index)
                        missingPartsCommonAncestorsQueue.Enqueue(bodyPartRecord.parts[index]);
                    
                }
                __instance.cachedMissingPartsCommonAncestors = cachedMissingPartsCommonAncestors;
            }
            return false;
        }

        public static bool Clear(HediffSet __instance)
        {
            __instance.hediffs = new List<Hediff>();
            __instance.DirtyCache();
            return false;
        }
        public static bool AddDirect(HediffSet __instance, Hediff hediff, DamageInfo? dinfo = null, DamageWorker.DamageResult damageResult = null)
        {
            if (hediff.def == null)
            {
                Log.Error("Tried to add health diff with null def. Canceling.");
                return false;
            }

            if (hediff.Part != null && !__instance.GetNotMissingParts().Contains(hediff.Part))
            {
                Log.Error("Tried to add health diff to missing part " + hediff.Part);
                return false;
            }

            hediff.ageTicks = 0;
            hediff.pawn = __instance.pawn;
            bool flag = false;
            lock (__instance)
            {
                for (int i = 0; i < __instance.hediffs.Count; i++)
                {
                    if (__instance.hediffs[i].TryMergeWith(hediff))
                    {
                        flag = true;
                    }
                }

                if (!flag)
                {
                    List<Hediff> newHediffs = new List<Hediff>(__instance.hediffs) { hediff };
                    __instance.hediffs = newHediffs;
                    //__instance.hediffs.Add(hediff);

                    hediff.PostAdd(dinfo);
                    __instance.pawn.needs?.mood?.thoughts.situational.Notify_SituationalThoughtsDirty();
                }
            }
            bool flag2 = hediff is Hediff_MissingPart;
            if (!(hediff is Hediff_MissingPart) && hediff.Part != null && hediff.Part != __instance.pawn.RaceProps.body.corePart && __instance.GetPartHealth(hediff.Part) == 0f && hediff.Part != __instance.pawn.RaceProps.body.corePart)
            {
                bool flag3 = __instance.HasDirectlyAddedPartFor(hediff.Part);
                Hediff_MissingPart hediff_MissingPart = (Hediff_MissingPart)HediffMaker.MakeHediff(HediffDefOf.MissingBodyPart, __instance.pawn);
                hediff_MissingPart.IsFresh = !flag3;
                hediff_MissingPart.lastInjury = hediff.def;
                __instance.pawn.health.AddHediff(hediff_MissingPart, hediff.Part, dinfo);
                damageResult?.AddHediff(hediff_MissingPart);
                if (flag3)
                {
                    if (dinfo.HasValue)
                    {
                        hediff_MissingPart.lastInjury = HealthUtility.GetHediffDefFromDamage(dinfo.Value.Def, __instance.pawn, hediff.Part);
                    }
                    else
                    {
                        hediff_MissingPart.lastInjury = null;
                    }
                }

                flag2 = true;
            }

            __instance.DirtyCache();
            if (flag2 && __instance.pawn.apparel != null)
            {
                __instance.pawn.apparel.Notify_LostBodyPart();
            }

            if (hediff.def.causesNeed != null && !__instance.pawn.Dead)
            {
                __instance.pawn.needs.AddOrRemoveNeedsAsAppropriate();
            }
            return false;
        }

        public static void DirtyCacheSetInvisbility(HediffSet __instance)
        {
            lock (PawnUtility_Patch.isPawnInvisible)
            {
                PawnUtility_Patch.RecalculateInvisibility(__instance.pawn);
            }
        }

    }
}

using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using System.Threading;
using UnityEngine;

namespace RimThreaded
{

    public class HediffSet_Patch
    {

        public static AccessTools.FieldRef<HediffSet, List<Hediff_MissingPart>> cachedMissingPartsCommonAncestors =
            AccessTools.FieldRefAccess<HediffSet, List<Hediff_MissingPart>>("cachedMissingPartsCommonAncestors");
        public static AccessTools.FieldRef<HediffSet, Queue<BodyPartRecord>> missingPartsCommonAncestorsQueue =
            AccessTools.FieldRefAccess<HediffSet, Queue<BodyPartRecord>>("missingPartsCommonAncestorsQueue");


        public static bool GetPartHealth(HediffSet __instance, ref float __result, BodyPartRecord part)
        {
            if (part == null)
            {
                __result = 0f;
                return false;
            }

            //---START ADD---
            if (part.def == null)
            {
                __result = 0f;
                return false;
            }
            //---END ADD---

            float num = part.def.GetMaxHealth(__instance.pawn);
            for (int i = 0; i < __instance.hediffs.Count; i++)
            {
                //---START ADD---
                Hediff hediff;
                try
                {
                    hediff = __instance.hediffs[i];
                }
                catch (ArgumentOutOfRangeException) {
                    break;
                }
                //---END ADD---

                //REPLACE hediffs[i] with hediff
                if (hediff is Hediff_MissingPart && hediff.Part == part)
                {
                    __result = 0f;
                    return false;
                }

                //REPLACE hediffs[i] with hediff
                if (hediff.Part == part)
                {
                    //REPLACE hediffs[i] with hediff
                    Hediff_Injury hediff_Injury = hediff as Hediff_Injury;
                    if (hediff_Injury != null)
                    {
                        num -= hediff_Injury.Severity;
                    }
                }
            }

            num = Mathf.Max(num, 0f);
            if (!part.def.destroyableByDamage)
            {
                num = Mathf.Max(num, 1f);
            }

            __result = Mathf.RoundToInt(num);
            return false;
        }

        public static bool HasImmunizableNotImmuneHediff(HediffSet __instance, ref bool __result)
        {
            __result = false;
            if (__instance.hediffs != null)
            {
                for (int i = __instance.hediffs.Count - 1; i >= 0; i--)
                {
                    Hediff hediff = null;
                    try
                    {
                        hediff = __instance.hediffs[i];
                    }
                    catch (ArgumentOutOfRangeException) {}
                    if (hediff != null)
                    {
                        if (!(hediff is Hediff_Injury) && !(hediff is Hediff_MissingPart) && hediff.Visible && hediff.def != null && hediff.def.PossibleToDevelopImmunityNaturally() && !hediff.FullyImmune())
                        {
                            __result = true;
                            return false;
                        }
                    }
                }
            }
            return false;
        }



        public static bool HasTendableHediff(HediffSet __instance, ref bool __result, bool forAlert = false)
        {
            if (__instance.hediffs != null)
            {
                for (int i = 0; i < __instance.hediffs.Count; i++)
                {
                    Hediff hediff = null;
                    try
                    {
                        hediff = __instance.hediffs[i];
                    }
                    catch (ArgumentOutOfRangeException) { break; }

                    if (hediff != null)
                    {
                        if ((!forAlert || (hediff.def != null && hediff.def.makesAlert)) && hediff.TendableNow())
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }



        public static bool GetFirstHediffOfDef(HediffSet __instance, ref Hediff __result, HediffDef def, bool mustBeVisible = false)
        {
            for (int i = 0; i < __instance.hediffs.Count; i++)
            {
                if (__instance.hediffs != null && __instance.hediffs[i] != null && __instance.hediffs[i].def == def && (!mustBeVisible || __instance.hediffs[i].Visible))
                {
                    __result = __instance.hediffs[i];
                    return false;
                }
            }

            return false;
        }

        public static bool PartIsMissing(HediffSet __instance, ref bool __result, BodyPartRecord part)
        {
            for (int i = 0; i < __instance.hediffs.Count; i++)
            {
                if (__instance.hediffs[i] != null && __instance.hediffs[i].Part == part && __instance.hediffs[i] is Hediff_MissingPart)
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
            for (int i = 0; i < __instance.hediffs.Count; i++)
            {
                Hediff hediff;
                try
                {
                    hediff = __instance.hediffs[i];
                } catch (ArgumentOutOfRangeException)
                {
                    break;
                }
                if (hediff != null && hediff.Part == part && hediff is Hediff_AddedPart)
                {
                    __result = true;
                    return false;
                }
            }
            __result = false;
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
            for (int i = 0; i < __instance.hediffs.Count; i++)
            {
                if (__instance.hediffs[i].TryMergeWith(hediff))
                {
                    flag = true;
                }
            }

            if (!flag)
            {
                lock (__instance)
                {
                    List<Hediff> newHediffs = new List<Hediff>(__instance.hediffs) { hediff };
                    __instance.hediffs = newHediffs;
                    //__instance.hediffs.Add(hediff);
                }
                hediff.PostAdd(dinfo);
                if (__instance.pawn.needs != null && __instance.pawn.needs.mood != null)
                {
                    __instance.pawn.needs.mood.thoughts.situational.Notify_SituationalThoughtsDirty();
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

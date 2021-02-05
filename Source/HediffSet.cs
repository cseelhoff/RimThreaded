using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        public static MethodInfo makeshiftEbfEndpoint = AccessTools.Method("EBF.VanillaExtender:GetMaxHealth");
        public static FastInvokeHandler myDelegate = null;

        public static bool GetPartHealth(HediffSet __instance, ref float __result, BodyPartRecord part)
        {
            if (part == null || part.def == null)
            {
                __result = 0f;
                return false;
            }

            float num = EBF_GetMaxHealth(part.def, __instance.pawn, part); //part.def.GetMaxHealth(__instance.pawn); 
            for (int i = __instance.hediffs.Count - 1; i >= 0; i--)
            {
                Hediff hediff = null;
                try
                {
                    hediff = __instance.hediffs[i];
                } catch(ArgumentOutOfRangeException) {}
                if (hediff != null && hediff is Hediff_MissingPart && hediff.Part == part)
                {
                    __result = 0f;
                    return false;
                }

                if (hediff != null && hediff.Part == part)
                {
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
        //Elite_Bionics_Framework_Patch
        public static float EBF_GetMaxHealth(BodyPartDef def, Pawn pawn, BodyPartRecord record)
        {
            if (makeshiftEbfEndpoint != null)
            {
                if (myDelegate == null)
                {
                    myDelegate = MethodInvoker.GetHandler(makeshiftEbfEndpoint);
                }
                return (float)myDelegate(null, new object[] { def, pawn, record });
            }
            return def.GetMaxHealth(pawn);
        }

    }
}

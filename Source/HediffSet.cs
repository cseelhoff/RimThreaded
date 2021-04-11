using System;
using System.Linq;
using RimWorld;
using Verse;

namespace RimThreaded
{

    public class HediffSet_Patch
    {

        internal static void RunDestructivePatches()
        {
            Type original = typeof(HediffSet);
            Type patched = typeof(HediffSet_Patch);
            RimThreadedHarmony.Prefix(original, patched, "AddDirect");
        }
        internal static void RunNonDestructivePatches()
        {
            Type original = typeof(HediffSet);
            Type patched = typeof(HediffSet_Patch);
            RimThreadedHarmony.Postfix(original, patched, "DirtyCache", "DirtyCacheSetInvisbility");
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
                    //List<Hediff> newHediffs = new List<Hediff>(__instance.hediffs) { hediff };
                    //__instance.hediffs = newHediffs;
                    __instance.hediffs.Add(hediff);
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

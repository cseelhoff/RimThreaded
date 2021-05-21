using System;
using System.Collections.Generic;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{

    public class JobGiver_OptimizeApparel_Patch
    {

        internal static void RunDestructivePatches()
        {
            Type original = typeof(JobGiver_OptimizeApparel);
            Type patched = typeof(JobGiver_OptimizeApparel_Patch);
            RimThreadedHarmony.Prefix(original, patched, "ApparelScoreGain");
            RimThreadedHarmony.Prefix(original, patched, "ApparelScoreGain_NewTmp");
            RimThreadedHarmony.Prefix(original, patched, "TryGiveJob");
        }
        
        public static bool ApparelScoreGain(ref float __result, Pawn pawn, Apparel ap)
        {
            List<float> wornApparelScores = new List<float>();
            //wornApparelScores.Clear();
            for (int i = 0; i < pawn.apparel.WornApparel.Count; i++)
            {
                Apparel apparel = pawn.apparel.WornApparel[i];
                wornApparelScores.Add(JobGiver_OptimizeApparel.ApparelScoreRaw(pawn, apparel));
            }
            float scoreGain = 0f;
            ApparelScoreGain_NewTmp(ref scoreGain, pawn, ap, wornApparelScores);
            __result = scoreGain;
            return false;
        }
        public static bool ApparelScoreGain_NewTmp(ref float __result, Pawn pawn, Apparel ap, List<float> wornScoresCache)
        {
            if (ap is ShieldBelt && pawn.equipment.Primary != null && pawn.equipment.Primary.def.IsWeaponUsingProjectiles)
            {
                __result = -1000f;
                return false;
            }

            float num = JobGiver_OptimizeApparel.ApparelScoreRaw(pawn, ap);
            List<Apparel> wornApparel = pawn.apparel.WornApparel;
            bool flag = false;
            for (int i = 0; i < wornApparel.Count; i++)
            {
                Apparel apparel;
                try
                {
                    apparel = wornApparel[i];
                }
                catch (ArgumentOutOfRangeException)
                {
                    break;
                }
                if (!ApparelUtility.CanWearTogether(apparel.def, ap.def, pawn.RaceProps.body))
                {
                    if (!pawn.outfits.forcedHandler.AllowedToAutomaticallyDrop(apparel) || pawn.apparel.IsLocked(apparel))
                    {
                        __result = -1000f;
                        return false;
                    }

                    num -= wornScoresCache[i];
                    flag = true;
                }
            }

            if (!flag)
            {
                num *= 10f;
            }

            __result = num;
            return false;
        }

        public static bool TryGiveJob(JobGiver_OptimizeApparel __instance, ref Job __result, Pawn pawn)
        {
            if (pawn.outfits == null)
            {
                Log.ErrorOnce(string.Concat(pawn, " tried to run JobGiver_OptimizeApparel without an OutfitTracker"), 5643897);
                __result = null;
                return false;
            }

            if (pawn.Faction != Faction.OfPlayer)
            {
                Log.ErrorOnce(string.Concat("Non-colonist ", pawn, " tried to optimize apparel."), 764323);
                __result = null;
                return false;
            }

            if (pawn.IsQuestLodger())
            {
                __result = null;
                return false;
            }

            if (!DebugViewSettings.debugApparelOptimize)
            {
                if (Find.TickManager.TicksGame < pawn.mindState.nextApparelOptimizeTick)
                {
                    __result = null;
                    return false;
                }
            }
            else
            {
                JobGiver_OptimizeApparel.debugSb = new StringBuilder();
                JobGiver_OptimizeApparel.debugSb.AppendLine(string.Concat("Scanning for ", pawn, " at ", pawn.Position));
            }

            Outfit currentOutfit = pawn.outfits.CurrentOutfit;
            List<Apparel> wornApparel = pawn.apparel.WornApparel;
            for (int num = wornApparel.Count - 1; num >= 0; num--)
            {
                if (!currentOutfit.filter.Allows(wornApparel[num]) && pawn.outfits.forcedHandler.AllowedToAutomaticallyDrop(wornApparel[num]) && !pawn.apparel.IsLocked(wornApparel[num]))
                {
                    Job job = JobMaker.MakeJob(JobDefOf.RemoveApparel, wornApparel[num]);
                    job.haulDroppedApparel = true;
                    __result = job;
                    return false;
                }
            }

            Thing thing = null;
            float num2 = 0f;
            List<Thing> list = pawn.Map.listerThings.ThingsInGroup(ThingRequestGroup.Apparel);
            if (list.Count == 0)
            {
                SetNextOptimizeTick2(pawn);
                __result = null;
                return false;
            }

            JobGiver_OptimizeApparel.neededWarmth = PawnApparelGenerator.CalculateNeededWarmth(pawn, pawn.Map.Tile, GenLocalDate.Twelfth(pawn));
            //wornApparelScores.Clear();
            List<float> wornApparelScores = new List<float>();
            for (int i = 0; i < wornApparel.Count; i++)
            {
                wornApparelScores.Add(JobGiver_OptimizeApparel.ApparelScoreRaw(pawn, wornApparel[i]));
            }

            for (int j = 0; j < list.Count; j++)
            {
                Apparel apparel = (Apparel)list[j];
                if (currentOutfit.filter.Allows(apparel) && apparel.IsInAnyStorage() && !apparel.IsForbidden(pawn) && !apparel.IsBurning() && (apparel.def.apparel.gender == Gender.None || apparel.def.apparel.gender == pawn.gender))
                {
                    float num3 = JobGiver_OptimizeApparel.ApparelScoreGain_NewTmp(pawn, apparel, wornApparelScores);
                    if (DebugViewSettings.debugApparelOptimize)
                    {
                        JobGiver_OptimizeApparel.debugSb.AppendLine(apparel.LabelCap + ": " + num3.ToString("F2"));
                    }

                    if (!(num3 < 0.05f) && !(num3 < num2) && (!EquipmentUtility.IsBiocoded(apparel) || EquipmentUtility.IsBiocodedFor(apparel, pawn)) && ApparelUtility.HasPartsToWear(pawn, apparel.def) && pawn.CanReserveAndReach(apparel, PathEndMode.OnCell, pawn.NormalMaxDanger()))
                    {
                        thing = apparel;
                        num2 = num3;
                    }
                }
            }

            if (DebugViewSettings.debugApparelOptimize)
            {
                JobGiver_OptimizeApparel.debugSb.AppendLine("BEST: " + thing);
                Log.Message(JobGiver_OptimizeApparel.debugSb.ToString());
                JobGiver_OptimizeApparel.debugSb = null;
            }

            if (thing == null)
            {
                SetNextOptimizeTick2(pawn);
                __result = null;
                return false;
            }

            __result = JobMaker.MakeJob(JobDefOf.Wear, thing);
            return false;
        }
        private static void SetNextOptimizeTick2(Pawn pawn)
        {
            pawn.mindState.nextApparelOptimizeTick = Find.TickManager.TicksGame + Rand.Range(6000, 9000);
        }

    }
}
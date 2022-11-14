using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace RimThreaded.RW_Patches
{
    public static class PawnUtility_Patch
    {
        public static Dictionary<Pawn, bool> isPawnInvisible = new Dictionary<Pawn, bool>();

        public static void RunDestructivePatches()
        {
            Type original = typeof(PawnUtility);
            Type patched = typeof(PawnUtility_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(IsInvisible));
            RimThreadedHarmony.Prefix(original, patched, nameof(PawnBlockingPathAt));
        }

        public static bool PawnBlockingPathAt(ref Pawn __result,
          IntVec3 c,
          Pawn forPawn,
          bool actAsIfHadCollideWithPawnsJob = false,
          bool collideOnlyWithStandingPawns = false,
          bool forPathFinder = false)
        {
            List<Thing> thingList = c.GetThingList(forPawn.Map);
            if (thingList.Count == 0 || forPawn == null) //added for 1.4
            {
                __result = null;
                return false;
            }
            bool flag = false;
            if (actAsIfHadCollideWithPawnsJob)
            {
                flag = true;
            }
            else
            {
                Job curJob = forPawn.CurJob;
                if (curJob != null && (curJob.collideWithPawns || curJob.def.collideWithPawns || forPawn.jobs.curDriver.collideWithPawns))
                    flag = true;
                /*
                else if (forPawn.Drafted)
                {
                    int num = forPawn.pather.Moving ? 1 : 0;
                }
                */
            }
            for (int index = 0; index < thingList.Count; ++index)
            {
                if (thingList[index] is Pawn pawn1 && pawn1 != forPawn && !pawn1.Downed && (!collideOnlyWithStandingPawns || !pawn1.pather.MovingNow && (!pawn1.pather.Moving || !pawn1.pather.MovedRecently(60))) && !PawnUtility.PawnsCanShareCellBecauseOfBodySize(pawn1, forPawn))
                {
                    if (pawn1.HostileTo(forPawn))
                    {
                        __result = pawn1;
                        return false;
                    }
                    if (flag && (forPathFinder || !forPawn.Drafted || !pawn1.RaceProps.Animal))
                    {
                        Job curJob = pawn1.CurJob;
                        if (curJob != null)
                        {
                            if (curJob.collideWithPawns)
                            {
                                __result = pawn1;
                                return false;
                            }
                            JobDef def = curJob.def;
                            if (def != null && def.collideWithPawns)
                            {
                                __result = pawn1;
                                return false;
                            }
                            if (pawn1 != null)
                            {
                                Pawn_JobTracker jobs = pawn1.jobs;
                                if (jobs != null)
                                {
                                    JobDriver curDriver = jobs.curDriver;
                                    if (curDriver != null && curDriver.collideWithPawns)
                                    {
                                        __result = pawn1;
                                        return false;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            __result = null;
            return false;
        }

        public static bool IsInvisible(ref bool __result, Pawn pawn)
        {
            if (!isPawnInvisible.TryGetValue(pawn, out bool isInvisible))
            {
                lock (isPawnInvisible)
                {
                    if (!isPawnInvisible.TryGetValue(pawn, out bool isInvisible2))
                    {
                        isInvisible = RecalculateInvisibility(pawn);
                    }
                    else
                    {
                        isInvisible = isInvisible2;
                    }
                }
            }
            __result = isInvisible;
            return false;
        }

        public static bool RecalculateInvisibility(Pawn pawn)
        {
            bool isInvisible = false;
            List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
            for (int i = 0; i < hediffs.Count; i++)
            {
                if (hediffs[i].TryGetComp<HediffComp_Invisibility>() != null)
                {
                    isInvisible = true;
                    break;
                }
            }
            isPawnInvisible[pawn] = isInvisible;
            return isInvisible;
        }


    }
}
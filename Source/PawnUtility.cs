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
	public static class PawnUtility_Patch
	{

		public static bool EnemiesAreNearby(ref bool __result, Pawn pawn, int regionsToScan = 9, bool passDoors = false)
		{
			TraverseParms tp = passDoors ? TraverseParms.For(TraverseMode.PassDoors, Danger.Deadly, false) : TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false);
			bool foundEnemy = false;
			RegionTraverser.BreadthFirstTraverse(pawn.Position, pawn.Map, (RegionEntryPredicate)((from, to) => to.Allows(tp, false)), (RegionProcessor)(r =>
			{
				Thing[] array;
				List<Thing> thingList = r.ListerThings.ThingsInGroup(ThingRequestGroup.AttackTarget);
				lock (thingList)
				{
					array = thingList.ToArray();
				}
                for (int index = 0; index < array.Length; ++index)
				{
					Thing t = array[index];
					if (null != t)
					{
						if (t.HostileTo((Thing)pawn))
						{
							foundEnemy = true;
							return true;
						}
					}
				}
				
				return foundEnemy;
			}), regionsToScan, RegionType.Set_Passable);
			__result = foundEnemy;
			return false;
		}

		private static bool PawnsCanShareCellBecauseOfBodySize(Pawn p1, Pawn p2)
		{
			if (p1.BodySize >= 1.5f || p2.BodySize >= 1.5f)
			{
				return false;
			}
			float num = p1.BodySize / p2.BodySize;
			if (num < 1f)
			{
				num = 1f / num;
			}
			return num > 3.57f;
		}
        public static bool PawnBlockingPathAt(ref Pawn __result, IntVec3 c, Pawn forPawn, bool actAsIfHadCollideWithPawnsJob = false, bool collideOnlyWithStandingPawns = false, bool forPathFinder = false)
        {
            //List<Thing> thingList = c.GetThingList(forPawn.Map);
            IEnumerable<Thing> thingList = forPawn.Map.thingGrid.ThingsAt(c);
            if (!thingList.Any())
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
                if (curJob != null)
                {
                    if (curJob.collideWithPawns || (curJob.def != null && curJob.def.collideWithPawns) || (forPawn.jobs != null && forPawn.jobs.curDriver != null && forPawn.jobs.curDriver.collideWithPawns))
                    {
                        flag = true;
                    }
                    else if (forPawn.Drafted)
                    {
                        _ = forPawn.pather.Moving;
                    }
                }
            }

            //for (int i = 0; i < thingList.Count; i++)
            foreach(Thing thing in thingList)
            {
                Pawn pawn = thing as Pawn;
                if (pawn == null || pawn == forPawn || pawn.Downed || (collideOnlyWithStandingPawns && (pawn.pather.MovingNow || (pawn.pather.Moving && pawn.pather.MovedRecently(60)))) || PawnsCanShareCellBecauseOfBodySize(pawn, forPawn))
                {
                    continue;
                }

                if (pawn.HostileTo(forPawn))
                {
                    __result = pawn;
                    return false;
                }

                if (flag && (forPathFinder || !forPawn.Drafted || !pawn.RaceProps.Animal))
                {
                    Job curJob2 = pawn.CurJob;
                    JobDriver curDriver = pawn.jobs.curDriver;
                    if (curJob2 != null && curDriver != null)
                    {
                        if (curJob2.collideWithPawns || curJob2.def.collideWithPawns || curDriver.collideWithPawns)
                        {
                            __result = pawn;
                            return false;
                        }
                    }
                }
            }

            __result = null;
            return false;
        }

        public static bool ForceWait(Pawn pawn, int ticks, Thing faceTarget = null, bool maintainPosture = false)
        {
            if (ticks <= 0)
            {
                Log.ErrorOnce("Forcing a wait for zero ticks", 47045639);
            }

            Job job = JobMaker.MakeJob(maintainPosture ? JobDefOf.Wait_MaintainPosture : JobDefOf.Wait, faceTarget);
            job.expiryInterval = ticks;
            Pawn_JobTracker jobs = pawn.jobs;
            if (jobs != null && job != null)
            {
                jobs.StartJob(job, JobCondition.InterruptForced, null, resumeCurJobAfterwards: true);
            }
            return false;
        }

    }
}
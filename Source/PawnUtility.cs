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
			List<Thing> thingList = c.GetThingList(forPawn.Map);
			if (thingList.Count == 0)
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
				{
					flag = true;
				}
				/*
				else if (forPawn.Drafted)
				{
					bool moving = forPawn.pather.Moving;
				}
				*/
			}
			for (int i = 0; i < thingList.Count; i++)
			{
				Pawn pawn = thingList[i] as Pawn;
				if (pawn != null && pawn != forPawn)
				{
                    Pawn_PathFollower pathFollower = pawn.pather;
                    if (!pawn.Downed && pathFollower != null)
                    {
						if (!collideOnlyWithStandingPawns ||
						(!pathFollower.MovingNow &&
						(!pathFollower.Moving || !pathFollower.MovedRecently(60))) &&
						!PawnsCanShareCellBecauseOfBodySize(pawn, forPawn))
						{
							if (pawn.HostileTo(forPawn))
							{
								__result = pawn;
								return false;
							}

							if (flag && (forPathFinder || !forPawn.Drafted || !pawn.RaceProps.Animal))
							{
								Job curJob2 = pawn.CurJob;
								if (curJob2 != null && (curJob2.collideWithPawns || curJob2.def.collideWithPawns || pawn.jobs.curDriver.collideWithPawns))
								{
									__result = pawn;
									return false;
								}
							}
						}
					}
				}
			}
			__result = null;
			return false;
		}
	}
}
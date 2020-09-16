using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RimThreaded
{
	public class Pawn_PathFollower_Patch
	{

		public static AccessTools.FieldRef<Pawn_PathFollower, LocalTargetInfo> destination =
			AccessTools.FieldRefAccess<Pawn_PathFollower, LocalTargetInfo>("destination");
        public static AccessTools.FieldRef<Pawn_PathFollower, Pawn> pawn =
            AccessTools.FieldRefAccess<Pawn_PathFollower, Pawn>("pawn");
        public static AccessTools.FieldRef<Pawn_PathFollower, bool> moving =
            AccessTools.FieldRefAccess<Pawn_PathFollower, bool>("moving");
        public static AccessTools.FieldRef<Pawn_PathFollower, PathEndMode> peMode =
            AccessTools.FieldRefAccess<Pawn_PathFollower, PathEndMode>("peMode");
        public static AccessTools.FieldRef<Pawn_PathFollower, IntVec3> lastCell =
            AccessTools.FieldRefAccess<Pawn_PathFollower, IntVec3>("lastCell");
        public static AccessTools.FieldRef<Pawn_PathFollower, int> cellsUntilClamor =
            AccessTools.FieldRefAccess<Pawn_PathFollower, int>("cellsUntilClamor");
        public static AccessTools.FieldRef<Pawn_PathFollower, int> foundPathWhichCollidesWithPawns =
            AccessTools.FieldRefAccess<Pawn_PathFollower, int>("foundPathWhichCollidesWithPawns");
        public static AccessTools.FieldRef<Pawn_PathFollower, int> foundPathWithDanger =
            AccessTools.FieldRefAccess<Pawn_PathFollower, int>("foundPathWithDanger");

        public static Dictionary<int, Dictionary<Map, PathFinder>> threadMapPathFinderDict = new Dictionary<int, Dictionary<Map, PathFinder>>();

		public static bool GenerateNewPath(Pawn_PathFollower __instance, ref PawnPath __result)
		{
			PathFinder pathFinder;
			lock (threadMapPathFinderDict)
			{
				__instance.lastPathedTargetPosition = destination(__instance).Cell;
				if (!threadMapPathFinderDict.TryGetValue(Thread.CurrentThread.ManagedThreadId, out Dictionary<Map, PathFinder> mapPathFinderDict))
				{
					mapPathFinderDict = new Dictionary<Map, PathFinder>();
					threadMapPathFinderDict.Add(Thread.CurrentThread.ManagedThreadId, mapPathFinderDict);
				}
				if(!mapPathFinderDict.TryGetValue(pawn(__instance).Map, out pathFinder)) {
					pathFinder = new PathFinder(pawn(__instance).Map);
					mapPathFinderDict.Add(pawn(__instance).Map, pathFinder);
				}
				__result = pathFinder.FindPath(pawn(__instance).Position, destination(__instance), pawn(__instance), peMode(__instance));
			}
			return false;
		}
        public static void StopDead(Pawn_PathFollower __instance)
        {
            if (__instance.curPath != null)
                __instance.curPath.ReleaseToPool();
            __instance.curPath = (PawnPath)null;
            moving(__instance) = false;
            __instance.nextCell = pawn(__instance).Position;
        }
        private static void PatherFailed(Pawn_PathFollower __instance)
        {
            StopDead(__instance);
            pawn(__instance).jobs.curDriver.Notify_PatherFailed();
        }

        public static bool NextCellDoorToWaitForOrManuallyOpen(Pawn_PathFollower __instance, ref Building_Door __result)
		{
			Building_Door buildingDoor = ThingGrid_Patch.ThingAt_Building_Door(pawn(__instance).Map.thingGrid, __instance.nextCell);
			__result = buildingDoor != null && buildingDoor.SlowsPawns && (!buildingDoor.Open || buildingDoor.TicksTillFullyOpened > 0) && buildingDoor.PawnCanOpen(pawn(__instance)) ? buildingDoor : (Building_Door)null;
            return false;
		}


        private static int CostToMoveIntoCell(Pawn pawn, IntVec3 c)
        {
            int a = (c.x == pawn.Position.x || c.z == pawn.Position.z ? pawn.TicksPerMoveCardinal : pawn.TicksPerMoveDiagonal) + pawn.Map.pathGrid.CalculatedCostAt(c, false, pawn.Position);
            Building edifice = c.GetEdifice(pawn.Map);
            if (edifice != null)
                a += (int)edifice.PathWalkCostFor(pawn);
            if (a > 450)
                a = 450;
            if (pawn.CurJob != null)
            {
                Pawn locomotionUrgencySameAs = pawn.jobs.curDriver.locomotionUrgencySameAs;
                if (locomotionUrgencySameAs != null && locomotionUrgencySameAs != pawn && locomotionUrgencySameAs.Spawned)
                {
                    int moveIntoCell = CostToMoveIntoCell(locomotionUrgencySameAs, c);
                    if (a < moveIntoCell)
                        a = moveIntoCell;
                }
                else
                {
                    switch (pawn.jobs.curJob.locomotionUrgency)
                    {
                        case LocomotionUrgency.Amble:
                            a *= 3;
                            if (a < 60)
                            {
                                a = 60;
                                break;
                            }
                            break;
                        case LocomotionUrgency.Walk:
                            a *= 2;
                            if (a < 50)
                            {
                                a = 50;
                                break;
                            }
                            break;
                        case LocomotionUrgency.Jog:
#pragma warning disable CS1717 // Assignment made to same variable
                            a = a;
#pragma warning restore CS1717 // Assignment made to same variable
                            break;
                        case LocomotionUrgency.Sprint:
                            a = Mathf.RoundToInt((float)a * 0.75f);
                            break;
                    }
                }
            }
            return Mathf.Max(a, 1);
        }

        public static bool SetupMoveIntoNextCell(Pawn_PathFollower __instance)
		{
			if (__instance.curPath.NodesLeftCount <= 1)
			{
				Log.Error(pawn(__instance).ToString() + " at " + (object)pawn(__instance).Position + " ran out of path nodes while pathing to " + (object)destination + ".", false);
                PatherFailed(__instance);
			}
			else
			{
                __instance.nextCell = __instance.curPath.ConsumeNextNode();
				if (!__instance.nextCell.Walkable(pawn(__instance).Map))
					Log.Error(pawn(__instance).ToString() + " entering " + (object)__instance.nextCell + " which is unwalkable.", false);
				int moveIntoCell = CostToMoveIntoCell(pawn(__instance), __instance.nextCell);
                __instance.nextCellCostTotal = (float)moveIntoCell;
                __instance.nextCellCostLeft = (float)moveIntoCell;
                ThingGrid_Patch.ThingAt_Building_Door(pawn(__instance).Map.thingGrid, __instance.nextCell)?.Notify_PawnApproaching(pawn(__instance), moveIntoCell);
			}
            return false;
        }

        private static bool BestPathHadPawnsInTheWayRecently(Pawn_PathFollower __instance)
        {
            return foundPathWhichCollidesWithPawns(__instance) + 240 > Find.TickManager.TicksGame;
        }
        private static bool BestPathHadDangerRecently(Pawn_PathFollower __instance)
        {
            return foundPathWithDanger(__instance) + 240 > Find.TickManager.TicksGame;
        }
        private static bool NeedNewPath(Pawn_PathFollower __instance)
        {
            if (!destination(__instance).IsValid || __instance.curPath == null || (!__instance.curPath.Found || __instance.curPath.NodesLeftCount == 0) || destination(__instance).HasThing &&
                destination(__instance).Thing.Map != pawn(__instance).Map || (pawn(__instance).Position.InHorDistOf(__instance.curPath.LastNode, 15f) || pawn(__instance).Position.InHorDistOf(destination(__instance).Cell, 15f)) && 
                !ReachabilityImmediate.CanReachImmediate(__instance.curPath.LastNode, destination(__instance), pawn(__instance).Map, peMode(__instance), pawn(__instance)) || 
                __instance.curPath.UsedRegionHeuristics && __instance.curPath.NodesConsumedCount >= 75)
                return true;
            if (__instance.lastPathedTargetPosition != destination(__instance).Cell)
            {
                float horizontalSquared = (float)(pawn(__instance).Position - destination(__instance).Cell).LengthHorizontalSquared;
                float num = (double)horizontalSquared <= 900.0 ? ((double)horizontalSquared <= 289.0 ? ((double)horizontalSquared <= 100.0 ? ((double)horizontalSquared <= 49.0 ? 0.5f : 2f) : 3f) : 5f) : 10f;
                if ((double)(__instance.lastPathedTargetPosition - destination(__instance).Cell).LengthHorizontalSquared > (double)num * (double)num)
                    return true;
            }
            bool flag1 = PawnUtility.ShouldCollideWithPawns(pawn(__instance));
            bool flag2 = __instance.curPath.NodesLeftCount < 30;
            IntVec3 other = IntVec3.Invalid;
            for (int nodesAhead = 0; nodesAhead < 20 && nodesAhead < __instance.curPath.NodesLeftCount; ++nodesAhead)
            {
                IntVec3 c = __instance.curPath.Peek(nodesAhead);
                if (!c.Walkable(pawn(__instance).Map) || flag1 && !BestPathHadPawnsInTheWayRecently(__instance) && 
                    (PawnUtility.AnyPawnBlockingPathAt(c, pawn(__instance), false, true, false) || 
                    flag2 && PawnUtility.AnyPawnBlockingPathAt(c, pawn(__instance), false, false, false)) || 
                    (!BestPathHadDangerRecently(__instance) && PawnUtility.KnownDangerAt(c, pawn(__instance).Map, pawn(__instance)) || 
                    c.GetEdifice(pawn(__instance).Map) is Building_Door edifice && (!edifice.CanPhysicallyPass(pawn(__instance)) && 
                    !pawn(__instance).HostileTo((Thing)edifice) || edifice.IsForbiddenToPass(pawn(__instance)))) || nodesAhead != 0 && 
                    c.AdjacentToDiagonal(other) && (PathFinder.BlocksDiagonalMovement(c.x, other.z, pawn(__instance).Map) ||
                    PathFinder.BlocksDiagonalMovement(other.x, c.z, pawn(__instance).Map)))
                    return true;
                other = c;
            }
            return false;
        }
        private static bool TrySetNewPath(Pawn_PathFollower __instance)
        {
            PawnPath newPath = null;
            GenerateNewPath(__instance, ref newPath);
            if (!newPath.Found)
            {
                PatherFailed(__instance);
                return false;
            }
            if (__instance.curPath != null)
                __instance.curPath.ReleaseToPool();
            __instance.curPath = newPath;
            for (int nodesAhead = 0; nodesAhead < 20 && nodesAhead < __instance.curPath.NodesLeftCount; ++nodesAhead)
            {
                IntVec3 c = __instance.curPath.Peek(nodesAhead);
                if (PawnUtility.ShouldCollideWithPawns(pawn(__instance)) && PawnUtility.AnyPawnBlockingPathAt(c, pawn(__instance), false, false, false))
                    foundPathWhichCollidesWithPawns(__instance) = Find.TickManager.TicksGame;
                if (PawnUtility.KnownDangerAt(c, pawn(__instance).Map, pawn(__instance)))
                    foundPathWithDanger(__instance) = Find.TickManager.TicksGame;
                if (foundPathWhichCollidesWithPawns(__instance) == Find.TickManager.TicksGame && foundPathWithDanger(__instance) == Find.TickManager.TicksGame)
                    break;
            }
            return true;
        }
        private static bool AtDestinationPosition(Pawn_PathFollower __instance)
        {
            return pawn(__instance).CanReachImmediate(destination(__instance), peMode(__instance));
        }
        private static void PatherArrived(Pawn_PathFollower __instance)
        {
            StopDead(__instance);
            if (pawn(__instance).jobs.curJob == null)
                return;
            pawn(__instance).jobs.curDriver.Notify_PatherArrived();
        }
        public static bool TryEnterNextPathCell(Pawn_PathFollower __instance, ref bool __result)
        {
            Building building = __instance.BuildingBlockingNextPathCell();
            if (building != null && (!(building is Building_Door buildingDoor) || !buildingDoor.FreePassage))
            {
                if (pawn(__instance).CurJob != null && pawn(__instance).CurJob.canBash || pawn(__instance).HostileTo((Thing)building))
                {
                    Job newJob = JobMaker.MakeJob(JobDefOf.AttackMelee, (LocalTargetInfo)(Thing)building);
                    newJob.expiryInterval = 300;
                    pawn(__instance).jobs.StartJob(newJob, JobCondition.Incompletable, (ThinkNode)null, false, true, (ThinkTreeDef)null, new JobTag?(), false, false);
                }
                else
                    PatherFailed(__instance);
            }
            else
            {
                Building_Door forOrManuallyOpen = null;
                NextCellDoorToWaitForOrManuallyOpen(__instance, ref forOrManuallyOpen);
                if (forOrManuallyOpen != null)
                {
                    if (!forOrManuallyOpen.Open)
                        forOrManuallyOpen.StartManualOpenBy(pawn(__instance));
                    Stance_Cooldown stanceCooldown = new Stance_Cooldown(forOrManuallyOpen.TicksTillFullyOpened, (LocalTargetInfo)(Thing)forOrManuallyOpen, (Verb)null);
                    stanceCooldown.neverAimWeapon = true;
                    pawn(__instance).stances.SetStance((Stance)stanceCooldown);
                    forOrManuallyOpen.CheckFriendlyTouched(pawn(__instance));
                }
                else
                {
                    lastCell(__instance) = pawn(__instance).Position;
                    pawn(__instance).Position = __instance.nextCell;
                    if (pawn(__instance).RaceProps.Humanlike)
                    {
                        --cellsUntilClamor(__instance);
                        if (cellsUntilClamor(__instance) <= 0)
                        {
                            GenClamor.DoClamor((Thing)pawn(__instance), 7f, ClamorDefOf.Movement);
                            cellsUntilClamor(__instance) = 12;
                        }
                    }
                    pawn(__instance).filth.Notify_EnteredNewCell();
                    if ((double)pawn(__instance).BodySize > 0.899999976158142)
                        pawn(__instance).Map.snowGrid.AddDepth(pawn(__instance).Position, -1f / 1000f);
                    Building_Door buildingDoor1 = ThingGrid_Patch.ThingAt_Building_Door(pawn(__instance).Map.thingGrid, lastCell(__instance));
                    if (buildingDoor1 != null && !pawn(__instance).HostileTo((Thing)buildingDoor1))
                    {
                        buildingDoor1.CheckFriendlyTouched(pawn(__instance));
                        if (!buildingDoor1.BlockedOpenMomentary && !buildingDoor1.HoldOpen && (buildingDoor1.SlowsPawns && buildingDoor1.PawnCanOpen(pawn(__instance))))
                        {
                            buildingDoor1.StartManualCloseBy(pawn(__instance));
                            return false;
                        }
                    }
                    if (NeedNewPath(__instance) && !TrySetNewPath(__instance))
                        return false;
                    if (AtDestinationPosition(__instance))
                        PatherArrived(__instance);
                    else
                        SetupMoveIntoNextCell(__instance);
                }
            }
            return false;
        }
        
    }
}
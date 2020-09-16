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

        public Building_Door NextCellDoorToWaitForOrManuallyOpen(Pawn_PathFollower __instance)
		{
			Building_Door buildingDoor = pawn(__instance).Map.thingGrid.ThingAt<Building_Door>(__instance.nextCell);
			return buildingDoor != null && buildingDoor.SlowsPawns && (!buildingDoor.Open || buildingDoor.TicksTillFullyOpened > 0) && buildingDoor.PawnCanOpen(pawn(__instance)) ? buildingDoor : (Building_Door)null;
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
                            a = a;
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
                pawn(__instance).Map.thingGrid.ThingAt<Building_Door>(__instance.nextCell)?.Notify_PawnApproaching(pawn(__instance), moveIntoCell);
			}
            return false;
        }
        /*
        public void StopDead(Pawn_PathFollower __instance)
        {
            if (__instance.curPath != null)
                __instance.curPath.ReleaseToPool();
            __instance.curPath = (PawnPath)null;
            moving(__instance) = false;
            __instance.nextCell = pawn(__instance).Position;
        }
                private void PatherFailed(Pawn_PathFollower __instance)
        {
            StopDead(__instance);
            pawn(__instance).jobs.curDriver.Notify_PatherFailed();
        }
        

        private bool NeedNewPath(Pawn_PathFollower __instance)
        {
            if (!__instance.destination.IsValid || __instance.curPath == null || (!__instance.curPath.Found || __instance.curPath.NodesLeftCount == 0) || __instance.destination.HasThing && __instance.destination.Thing.Map != __instance.pawn.Map || (__instance.pawn.Position.InHorDistOf(__instance.curPath.LastNode, 15f) || __instance.pawn.Position.InHorDistOf(__instance.destination.Cell, 15f)) && !ReachabilityImmediate.CanReachImmediate(__instance.curPath.LastNode, __instance.destination, __instance.pawn.Map, __instance.peMode, __instance.pawn) || __instance.curPath.UsedRegionHeuristics && __instance.curPath.NodesConsumedCount >= 75)
                return true;
            if (__instance.lastPathedTargetPosition != __instance.destination.Cell)
            {
                float horizontalSquared = (float)(__instance.pawn.Position - __instance.destination.Cell).LengthHorizontalSquared;
                float num = (double)horizontalSquared <= 900.0 ? ((double)horizontalSquared <= 289.0 ? ((double)horizontalSquared <= 100.0 ? ((double)horizontalSquared <= 49.0 ? 0.5f : 2f) : 3f) : 5f) : 10f;
                if ((double)(__instance.lastPathedTargetPosition - __instance.destination.Cell).LengthHorizontalSquared > (double)num * (double)num)
                    return true;
            }
            bool flag1 = PawnUtility.ShouldCollideWithPawns(pawn(__instance));
            bool flag2 = __instance.curPath.NodesLeftCount < 30;
            IntVec3 other = IntVec3.Invalid;
            for (int nodesAhead = 0; nodesAhead < 20 && nodesAhead < __instance.curPath.NodesLeftCount; ++nodesAhead)
            {
                IntVec3 c = __instance.curPath.Peek(nodesAhead);
                if (!c.Walkable(pawn(__instance).Map) || flag1 && !this.BestPathHadPawnsInTheWayRecently() && (PawnUtility.AnyPawnBlockingPathAt(c, this.pawn, false, true, false) || flag2 && PawnUtility.AnyPawnBlockingPathAt(c, this.pawn, false, false, false)) || (!this.BestPathHadDangerRecently() && PawnUtility.KnownDangerAt(c, this.pawn.Map, this.pawn) || c.GetEdifice(this.pawn.Map) is Building_Door edifice && (!edifice.CanPhysicallyPass(this.pawn) && !this.pawn.HostileTo((Thing)edifice) || edifice.IsForbiddenToPass(this.pawn))) || nodesAhead != 0 && c.AdjacentToDiagonal(other) && (PathFinder.BlocksDiagonalMovement(c.x, other.z, this.pawn.Map) || PathFinder.BlocksDiagonalMovement(other.x, c.z, this.pawn.Map)))
                    return true;
                other = c;
            }
            return false;
        }
        private bool TrySetNewPath(Pawn_PathFollower __instance)
        {
            PawnPath newPath = GenerateNewPath(__instance);
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
                    __instance.foundPathWhichCollidesWithPawns = Find.TickManager.TicksGame;
                if (PawnUtility.KnownDangerAt(c, pawn(__instance).Map, pawn(__instance)))
                    __instance.foundPathWithDanger = Find.TickManager.TicksGame;
                if (__instance.foundPathWhichCollidesWithPawns == Find.TickManager.TicksGame && __instance.foundPathWithDanger == Find.TickManager.TicksGame)
                    break;
            }
            return true;
        }

        private void TryEnterNextPathCell(Pawn_PathFollower __instance)
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
                Building_Door forOrManuallyOpen = NextCellDoorToWaitForOrManuallyOpen(__instance);
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
                    Building_Door buildingDoor1 = pawn(__instance).Map.thingGrid.ThingAt<Building_Door>(lastCell(__instance));
                    if (buildingDoor1 != null && !pawn(__instance).HostileTo((Thing)buildingDoor1))
                    {
                        buildingDoor1.CheckFriendlyTouched(pawn(__instance));
                        if (!buildingDoor1.BlockedOpenMomentary && !buildingDoor1.HoldOpen && (buildingDoor1.SlowsPawns && buildingDoor1.PawnCanOpen(pawn(__instance))))
                        {
                            buildingDoor1.StartManualCloseBy(pawn(__instance));
                            return;
                        }
                    }
                    if (NeedNewPath(__instance) && !TrySetNewPath(__instance))
                        return;
                    if (AtDestinationPosition(__instance))
                        PatherArrived(__instance);
                    else
                        SetupMoveIntoNextCell(__instance);
                }
            }
        }
        */
    }
}
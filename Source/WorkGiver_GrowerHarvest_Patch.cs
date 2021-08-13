using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using static RimThreaded.JumboCell;

namespace RimThreaded
{
    class WorkGiver_GrowerHarvest_Patch
    {
		public static IntVec3 ClosestLocationReachable(WorkGiver_GrowerHarvest workGiver_GrowerHarvest, Pawn pawn)
		{
			Danger maxDanger = pawn.NormalMaxDanger();
			Map map = pawn.Map;
			ZoneManager zoneManager = pawn.Map.zoneManager;
			foreach (IntVec3 actionableLocation in GetClosestActionableLocations(pawn, map, RimThreaded.plantHarvest_Cache))
			{
				List<Thing> thingsAtLocation = GridsUtility.GetThingList(actionableLocation, map);
				foreach (Thing thingAtLocation in thingsAtLocation)
				{
					if (thingAtLocation is Building_PlantGrower building_PlantGrower)
					{
						if (building_PlantGrower == null || !workGiver_GrowerHarvest.ExtraRequirements(building_PlantGrower, pawn)
							|| building_PlantGrower.IsForbidden(pawn)
							|| !pawn.CanReach(building_PlantGrower, PathEndMode.OnCell, maxDanger)
							)
						{
							continue;
						}
						Plant plant = actionableLocation.GetPlant(pawn.Map);
						bool hasJobOnCell = plant != null && (plant.HarvestableNow && plant.LifeStage == PlantLifeStage.Mature) && (plant.CanYieldNow());
						if (!hasJobOnCell)
						{
							break;
						}
						bool canReserve = ReservationManager_Patch.IsUnreserved(map.reservationManager, plant);
						if (!canReserve)
							break;
						return actionableLocation;
					}
				}
				if (!(zoneManager.ZoneAt(actionableLocation) is Zone_Growing growZone))
				{
					continue;
				}
				if (!workGiver_GrowerHarvest.ExtraRequirements(growZone, pawn))
				{
					continue;
				}
				if (!workGiver_GrowerHarvest.HasJobOnCell(pawn, actionableLocation))
				{
					JumboCell.ReregisterObject(pawn.Map, actionableLocation, RimThreaded.plantHarvest_Cache);
					continue;
				}
				if (!pawn.CanReach(actionableLocation, PathEndMode.OnCell, maxDanger))
				{
					continue;
				}
				return actionableLocation;

			}
			return IntVec3.Invalid;
		}
	}
}

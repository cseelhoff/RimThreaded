using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace RimThreaded
{
    class WorkGiver_GrowerHarvest_Patch
    {
		internal static IntVec3 ClosestLocationReachable(WorkGiver_GrowerHarvest workGiver_GrowerHarvest, Pawn pawn)
		{
			Danger maxDanger = pawn.NormalMaxDanger();
			//bool forced = false;
			Map map = pawn.Map;
			ZoneManager zoneManager = pawn.Map.zoneManager;
			foreach (IntVec3 actionableLocation in PlantHarvest_Cache.GetClosestActionableLocations(pawn, map, PlantHarvest_Cache.awaitingHarvestCellsMapDict))
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
				//if (!JobOnCellTest(workGiver_GrowerHarvest, pawn, actionableLocation, forced))
				//{
				//	continue;
				//}
				if (!workGiver_GrowerHarvest.HasJobOnCell(pawn, actionableLocation))
				{
					PlantHarvest_Cache.ReregisterObject(pawn.Map, actionableLocation, PlantHarvest_Cache.awaitingHarvestCellsMapDict);
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
		//public static bool JobOnCellTest(WorkGiver_GrowerHarvest workGiver_GrowerHarvest, Pawn pawn, IntVec3 c, bool forced = false)
		//{
		//	Map map = pawn.Map;
		//	if (workGiver_GrowerHarvest.HasJobOnCell(pawn, c, false))
		//	{
		//		Plant plant = c.GetPlant(map);
		//		if (!(plant.def == WorkGiver_Grower.CalculateWantedPlantDef(c, map)))
		//		{
		//			return true;
		//		}
		//	}
		//	return false;
		//}
	}
}

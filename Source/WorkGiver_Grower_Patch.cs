using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Verse;
using Verse.AI;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{
	class WorkGiver_Grower_Patch
	{
		public static Dictionary<Map, List<HashSet<object>[]>> awaitingPlantCellsMapDict = new Dictionary<Map, List<HashSet<object>[]>>();

		private static readonly MethodInfo methodExtraRequirements =
			Method(typeof(WorkGiver_Grower), "ExtraRequirements", new Type[] { typeof(IPlantToGrowSettable), typeof(Pawn) });
		private static readonly Func<WorkGiver_Grower, IPlantToGrowSettable, Pawn, bool> funcExtraRequirements =
			(Func<WorkGiver_Grower, IPlantToGrowSettable, Pawn, bool>)Delegate.CreateDelegate(typeof(Func<WorkGiver_Grower, IPlantToGrowSettable, Pawn, bool>), methodExtraRequirements);

		public static bool PotentialWorkCellsGlobal(WorkGiver_Grower __instance, ref IEnumerable<IntVec3> __result, Pawn pawn)
		{
			__result = PotentialWorkCellsGlobalIE(__instance, pawn);
			return false;
		}
		private static IEnumerable<IntVec3> PotentialWorkCellsGlobalIE(WorkGiver_Grower __instance, Pawn pawn)
		{
			Danger maxDanger = pawn.NormalMaxDanger();
			List<Building> bList = pawn.Map.listerBuildings.allBuildingsColonist;
			for (int j = 0; j < bList.Count; j++)
			{
				Building_PlantGrower building_PlantGrower = bList[j] as Building_PlantGrower;
				if (building_PlantGrower == null || !funcExtraRequirements(__instance, building_PlantGrower, pawn) || building_PlantGrower.IsForbidden(pawn) ||
					//!pawn.CanReach(building_PlantGrower, PathEndMode.OnCell, maxDanger) || 
					building_PlantGrower.IsBurning())
				{
					continue;
				}
				foreach (IntVec3 item in building_PlantGrower.OccupiedRect())
				{
					yield return item;
				}
			}
			List<Zone> zonesList = pawn.Map.zoneManager.AllZones;
			for (int j = 0; j < zonesList.Count; j++)
			{
				Zone_Growing growZone = zonesList[j] as Zone_Growing;
				if (growZone == null)
				{
					continue;
				}
				if (growZone.cells.Count == 0)
				{
					Log.ErrorOnce("Grow zone has 0 cells: " + growZone, -563487);
				}
				else if (funcExtraRequirements(__instance, growZone, pawn) && !growZone.ContainsStaticFire &&
					pawn.CanReach(growZone.Cells[0], PathEndMode.OnCell, maxDanger))
				{
					for (int k = 0; k < growZone.cells.Count; k++)
					{
						yield return growZone.cells[k];
					}
				}
			}
		}
		public static IEnumerable<IntVec3> PotentialWorkCellsGlobalWithoutCanReach(WorkGiver_Grower __instance, Pawn pawn)
		{
			Danger maxDanger = pawn.NormalMaxDanger();
			List<Building> bList = pawn.Map.listerBuildings.allBuildingsColonist;
			for (int j = 0; j < bList.Count; j++)
			{
				Building_PlantGrower building_PlantGrower = bList[j] as Building_PlantGrower;
				if (building_PlantGrower == null || !funcExtraRequirements(__instance, building_PlantGrower, pawn) || building_PlantGrower.IsForbidden(pawn) || building_PlantGrower.IsBurning())
				{
					continue;
				}
				foreach (IntVec3 item in building_PlantGrower.OccupiedRect())
				{
					yield return item;
				}
			}
			List<Zone> zonesList = pawn.Map.zoneManager.AllZones;
			for (int j = 0; j < zonesList.Count; j++)
			{
				Zone_Growing growZone = zonesList[j] as Zone_Growing;
				if (growZone == null)
				{
					continue;
				}
				if (growZone.cells.Count == 0)
				{
					Log.ErrorOnce("Grow zone has 0 cells: " + growZone, -563487);
				}
				else if (funcExtraRequirements(__instance, growZone, pawn) && !growZone.ContainsStaticFire)
				{
					for (int k = 0; k < growZone.cells.Count; k++)
					{
						yield return growZone.cells[k];
					}
				}
			}
		}
		public static IEnumerable<IntVec3> ClosestPotentialWorkCellsGlobalWithoutCanReach(WorkGiver_Grower __instance, Pawn pawn)
		{
			Danger maxDanger = pawn.NormalMaxDanger();
			foreach (IntVec3 cell in RimThreaded.GetClosestPlantGrowerCells(pawn.Position))
			{
				List<Thing> thingsAtCell = pawn.Map.thingGrid.ThingsListAtFast(cell);
				foreach (Thing thingAtCell in thingsAtCell)
				{
                    if (!(thingAtCell is Building_PlantGrower building_PlantGrower) || 
						!funcExtraRequirements(__instance, building_PlantGrower, pawn) || 
						building_PlantGrower.IsForbidden(pawn) || 
						building_PlantGrower.IsBurning())
                    {
                        continue;
                    }
					yield return cell;
					break;
				}
                if (!(pawn.Map.zoneManager.ZoneAt(cell) is Zone_Growing growZone))
                {
                    continue;
                }
                if (growZone.cells.Count == 0)
				{
					Log.ErrorOnce("Grow zone has 0 cells: " + growZone, -563487);
				}
				else if (funcExtraRequirements(__instance, growZone, pawn) && !growZone.ContainsStaticFire)
				{
					yield return cell;
				}
			}
		}

        internal static IntVec3 ClosestLocationReachable(WorkGiver_Grower workGiver_Grower, Pawn pawn)
        {
			Danger maxDanger = pawn.NormalMaxDanger();
			//wantedPlantDef = null;
			//List<Zone> zonesList = pawn.Map.zoneManager.AllZones;
			//for (int j = 0; j < zonesList.Count; j++)
			//{

			//if (growZone.cells.Count == 0)
			//{
			//Log.ErrorOnce("Grow zone has 0 cells: " + growZone, -563487);
			//}
			bool forced = false;
			Map map = pawn.Map;
			ZoneManager zoneManager = pawn.Map.zoneManager;
			foreach (object obj in JumboCellCache.GetClosestActionableObjects(pawn, map, awaitingPlantCellsMapDict))
			{
				
				if(obj is Building_PlantGrower building_PlantGrower)
                {
					if (building_PlantGrower == null || !funcExtraRequirements(workGiver_Grower, building_PlantGrower, pawn) 
						|| building_PlantGrower.IsForbidden(pawn) 
						|| !pawn.CanReach(building_PlantGrower, PathEndMode.OnCell, maxDanger)
						//|| building_PlantGrower.IsBurning()
						)
					{
						continue;
					}

					foreach (IntVec3 item in building_PlantGrower.OccupiedRect())
					{
						return item; //TODO ADD check
					}
				}
				else  if (obj is IntVec3 c)
				
				{
					if (!(zoneManager.ZoneAt(c) is Zone_Growing growZone))
					{
						continue;
					}
					if (!funcExtraRequirements(workGiver_Grower, growZone, pawn))
					{
						continue;
					}
					if (!JobOnCellTest(workGiver_Grower.def, pawn, c, forced))
					{
						continue;
					}
					//!growZone.ContainsStaticFire && 
					if (!workGiver_Grower.HasJobOnCell(pawn, c))
					{
						continue;
					}
					if (!pawn.CanReach(c, PathEndMode.OnCell, maxDanger))
					{
						continue;
					}
					return c;
				}
			}
			//wantedPlantDef = null;
			return IntVec3.Invalid;
		}
		private static bool JobOnCellTest(WorkGiverDef def, Pawn pawn, IntVec3 c, bool forced = false)
		{
			Map map = pawn.Map;
			if (c.IsForbidden(pawn))
			{
				Log.Warning("IsForbidden");
				JumboCellCache.ReregisterObject(map, c, c, awaitingPlantCellsMapDict);
				return false;
			}

			if (!PlantUtility.GrowthSeasonNow(c, map, forSowing: true))
			{
				Log.Warning("GrowthSeasonNow");
				return false;
			}

			ThingDef localWantedPlantDef = WorkGiver_Grower.CalculateWantedPlantDef(c, map);
			WorkGiver_GrowerSow_Patch.wantedPlantDef = localWantedPlantDef;
			if (localWantedPlantDef == null)
			{
				Log.Warning("localWantedPlantDef==null");
				return false;
			}

			List<Thing> thingList = c.GetThingList(map);
			bool flag = false;
			for (int i = 0; i < thingList.Count; i++)
			{
				Thing thing = thingList[i];
				if (thing.def == localWantedPlantDef)
				{
					Log.Warning("thing.def == localWantedPlantDef... RemoveObjectFromAwaitingHaulingHashSets");

					JumboCellCache.ReregisterObject(map, c, c, awaitingPlantCellsMapDict);
					//JumboCellCache.AddObjectToActionableObjects(map, c, c, awaitingPlantCellsMapDict);
					return false;
				}

				if ((thing is Blueprint || thing is Frame) && thing.Faction == pawn.Faction)
				{
					flag = true;
				}
			}

			if (flag)
			{
				Thing edifice = c.GetEdifice(map);
				if (edifice == null || edifice.def.fertility < 0f)
				{
					Log.Warning("fertility");
					return false;
				}
			}

			if (localWantedPlantDef.plant.cavePlant)
			{
				if (!c.Roofed(map))
				{
					Log.Warning("cavePlant");
					return false;
				}

				if (map.glowGrid.GameGlowAt(c, ignoreCavePlants: true) > 0f)
				{
					Log.Warning("GameGlowAt");
					return false;
				}
			}

			if (localWantedPlantDef.plant.interferesWithRoof && c.Roofed(pawn.Map))
			{
				return false;
			}

			Plant plant = c.GetPlant(map);
			if (plant != null && plant.def.plant.blockAdjacentSow)
			{
				if (!pawn.CanReserve(plant, 1, -1, null, forced) || plant.IsForbidden(pawn))
				{
					Log.Warning("blockAdjacentSow");
					return false;
				}

				return true; // JobMaker.MakeJob(JobDefOf.CutPlant, plant);
			}

			Thing thing2 = PlantUtility.AdjacentSowBlocker(localWantedPlantDef, c, map);
			if (thing2 != null)
			{
				Plant plant2 = thing2 as Plant;
				if (plant2 != null && pawn.CanReserve(plant2, 1, -1, null, forced) && !plant2.IsForbidden(pawn))
				{
					IPlantToGrowSettable plantToGrowSettable = plant2.Position.GetPlantToGrowSettable(plant2.Map);
					if (plantToGrowSettable == null || plantToGrowSettable.GetPlantDefToGrow() != plant2.def)
					{
						return true; // JobMaker.MakeJob(JobDefOf.CutPlant, plant2);
					}
				}
				Log.Warning("AdjacentSowBlocker");
				JumboCellCache.ReregisterObject(map, c, c, awaitingPlantCellsMapDict);
				return false;
			}

			if (localWantedPlantDef.plant.sowMinSkill > 0 && pawn.skills != null && pawn.skills.GetSkill(SkillDefOf.Plants).Level < localWantedPlantDef.plant.sowMinSkill)
			{
				Log.Warning("UnderAllowedSkill");
				return false;
			}

			for (int j = 0; j < thingList.Count; j++)
			{
				Thing thing3 = thingList[j];
				if (!thing3.def.BlocksPlanting())
				{
					continue;
				}

				if (!pawn.CanReserve(thing3, 1, -1, null, forced))
				{
					Log.Warning("!CanReserve");
					JumboCellCache.ReregisterObject(map, c, c, awaitingPlantCellsMapDict);

					return false;
				}

				if (thing3.def.category == ThingCategory.Plant)
				{
					if (!thing3.IsForbidden(pawn))
					{
						return true; // JobMaker.MakeJob(JobDefOf.CutPlant, thing3);
					}
					Log.Warning("Plant IsForbidden");
					JumboCellCache.ReregisterObject(map, c, c, awaitingPlantCellsMapDict);

					return false;
				}

				if (thing3.def.EverHaulable)
				{
					return true; //HaulAIUtility.HaulAsideJobFor(pawn, thing3);
				}
				Log.Warning("EverHaulable");
				JumboCellCache.ReregisterObject(map, c, c, awaitingPlantCellsMapDict);
				return false;
			}

			if (!localWantedPlantDef.CanEverPlantAt_NewTemp(c, map))
			{
				Log.Warning("CanEverPlantAt_NewTemp");
				JumboCellCache.ReregisterObject(map, c, c, awaitingPlantCellsMapDict);
				return false;
			}

			if (!PlantUtility.GrowthSeasonNow(c, map, forSowing: true))
			{
				Log.Warning("GrowthSeasonNow");
				return false;
			}

			if (!pawn.CanReserve(c, 1, -1, null, forced))
			{
				Log.Warning("!pawn.CanReserve(c");
				JumboCellCache.ReregisterObject(map, c, c, awaitingPlantCellsMapDict);
				//JumboCellCache.AddObjectToActionableObjects(map, c, c, awaitingPlantCellsMapDict);
				return false;
			}

			//Job job = JobMaker.MakeJob(JobDefOf.Sow, c);
			//job.plantDefToSow = wantedPlantDef;
			return true; //job;
		}
	}
}
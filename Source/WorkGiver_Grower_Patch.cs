using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using Verse.AI;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{
	class WorkGiver_Grower_Patch
	{
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
				if (building_PlantGrower == null || !funcExtraRequirements(__instance, building_PlantGrower, pawn) || building_PlantGrower.IsForbidden(pawn) ||	building_PlantGrower.IsBurning())
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
	}
}
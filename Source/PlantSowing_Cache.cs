using RimWorld;
using System.Collections.Generic;
using Verse;

namespace RimThreaded
{
    public class PlantSowing_Cache : JumboCell_Cache
    {
		public override bool IsActionableObject(Map map, IntVec3 location)
		{
			//---START--- For plant sowing
			ThingDef localWantedPlantDef = WorkGiver_Grower.CalculateWantedPlantDef(location, map);
			if (localWantedPlantDef == null)
			{
				return false;
			}
			List<Thing> thingList = location.GetThingList(map);
			for (int i = 0; i < thingList.Count; i++)
			{
				Thing thing = thingList[i];
				if (thing.def == localWantedPlantDef)
				{
					return false;
				}
			}
			if (map.physicalInteractionReservationManager.IsReserved(location))
			{
				return false;
			}
			Thing thing2 = PlantUtility.AdjacentSowBlocker(localWantedPlantDef, location, map);
			if (thing2 != null)
			{
				if (thing2 is Plant plant2 && !plant2.IsForbidden(Faction.OfPlayer))
				{
					IPlantToGrowSettable plantToGrowSettable = plant2.Position.GetPlantToGrowSettable(plant2.Map);
					if (plantToGrowSettable != null && plantToGrowSettable.GetPlantDefToGrow() == plant2.def)
					{
						return false;
					}
				}
			}

			for (int j = 0; j < thingList.Count; j++)
			{
				Thing thing3 = thingList[j];
				if (!thing3.def.BlocksPlanting())
				{
					continue;
				}

				if (thing3.def.category == ThingCategory.Plant)
				{
					if (!thing3.IsForbidden(Faction.OfPlayer))
					{
						break; // JobMaker.MakeJob(JobDefOf.CutPlant, thing3);
					}
					Log.Warning("Plant IsForbidden");
					return false;
				}

				if (thing3.def.EverHaulable)
				{
					break; //HaulAIUtility.HaulAsideJobFor(pawn, thing3);
				}
				return false;
			}

			//TODO fix null check? find root cause. Or maybe it was just from a bad save?
			//if (localWantedPlantDef != null &&!localWantedPlantDef.CanEverPlantAt_NewTemp(location, map, true))
			// this change helps with boulders blocking growing zones. likely at a small performance cost
			if (localWantedPlantDef != null && (!location.InBounds(map) || (double)map.fertilityGrid.FertilityAt(location) < localWantedPlantDef.plant.fertilityMin))
			{
				return false;
			}
			//---END--
			return true;
		}
	}
}

using RimThreaded.RW_Patches;
using RimWorld;
using Verse;

namespace RimThreaded
{
    public class PlantHarvest_Cache : JumboCell_Cache
	{
		public override bool IsActionableObject(Map map, IntVec3 location)
        {
			//---START--- For plant Harvest
			//WorkGiver_GrowerHarvest.HasJobOnCell
			Plant plant = location.GetPlant(map);
			bool hasJobOnCell = plant != null && !plant.IsForbidden(Faction.OfPlayer) && (plant.HarvestableNow && plant.LifeStage == PlantLifeStage.Mature) && (plant.CanYieldNow());
			if (!hasJobOnCell)
			{
				return false;
			}
			bool canReserve = ReservationManager_Patch.IsUnreserved(map.reservationManager, plant);
			if (!canReserve)
				return false;
			//---END--
			return true;
		}
	}

}

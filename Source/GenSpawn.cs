using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimThreaded
{

    public class GenSpawn_Patch
	{
        public static bool WipeExistingThings(
          IntVec3 thingPos,
          Rot4 thingRot,
          BuildableDef thingDef,
          Map map,
          DestroyMode mode)
        {
            foreach (IntVec3 c in GenAdj.CellsOccupiedBy(thingPos, thingRot, thingDef.Size))
            {
                foreach (Thing thing in map.thingGrid.ThingsAt(c))
                {
                    if (null != thing)
                    {
                        ThingDef thingDef2 = thing.def;
                        if (null != thingDef2)
                        {
                            if (GenSpawn.SpawningWipes(thingDef, thingDef2))
                                thing.Destroy(mode);
                        }
                    }
                }                
            }
            return false;
        }

        public static bool CheckMoveItemsAside(
          IntVec3 thingPos,
          Rot4 thingRot,
          ThingDef thingDef,
          Map map)
        {
            if (thingDef.surfaceType != SurfaceType.None || thingDef.passability == Traversability.Standable)
            {                
                return false;
            }
            CellRect occupiedRect = GenAdj.OccupiedRect(thingPos, thingRot, thingDef.Size);
            foreach (IntVec3 intVec3 in occupiedRect)
            {
                //if (intVec3.InBounds(map))
                //{
                    //CellIndices cellIndices = map.cellIndices;
                    //List<Thing> list = intVec3.GetThingList(map).ToList<Thing>(); // map.thingGrid [cellIndices.CellToIndex(intVec3)];
                    foreach (Thing thing in map.thingGrid.ThingsAt(intVec3))
                    {
                        if (thing.def.category == ThingCategory.Item)
                        {
                            thing.DeSpawn();
                            if (!GenPlace.TryPlaceThing(thing, intVec3, map, ThingPlaceMode.Near, null, (x => !occupiedRect.Contains(x))))
                                thing.Destroy();
                        }
                    }                    
                //}
            }
            return false;
        }

    }
}

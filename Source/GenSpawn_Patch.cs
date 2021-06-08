
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimThreaded
{
    class GenSpawn_Patch
    {
        private static readonly Type Original = typeof(GenSpawn);
        private static readonly Type Patched = typeof(GenSpawn_Patch);

        public static void RunDestructivePatches()
        {
            RimThreadedHarmony.Prefix(Original, Patched, "WipeExistingThings");
        }

        public static bool WipeExistingThings(IntVec3 thingPos, Rot4 thingRot, BuildableDef thingDef, Map map, DestroyMode mode)
        {
            foreach (IntVec3 item in GenAdj.CellsOccupiedBy(thingPos, thingRot, thingDef.Size))
            {
                List<Thing> list = map.thingGrid.ThingsAt(item).ToList();
                for (int index = 0; index < list.Count; index++)
                {
                    Thing item2 = list[index];
                    if (item2 == null) continue;
                    if (GenSpawn.SpawningWipes(thingDef, item2.def))
                    {
                        item2.Destroy(mode);
                    }
                }
            }

            return false;
        }
	}
}

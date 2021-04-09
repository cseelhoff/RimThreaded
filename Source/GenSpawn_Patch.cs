using System;
using System.Linq;
using Verse;
using Verse.AI;

namespace RimThreaded
{
    class GenSpawn_Patch
    {
        internal static void RunDestructivePatches()
        {
            Type original = typeof(GenSpawn);
            Type patched = typeof(GenSpawn_Patch);
            RimThreadedHarmony.Prefix(original, patched, "WipeExistingThings");
        }
        public static bool WipeExistingThings(IntVec3 thingPos, Rot4 thingRot, BuildableDef thingDef, Map map, DestroyMode mode)
        {
            lock (map) //not needed i think.
            {
                if(thingDef?.Size != null)
                {
                    foreach (IntVec3 c in GenAdj.CellsOccupiedBy(thingPos, thingRot, thingDef.Size))
                    {
                        foreach (Thing thing in map.thingGrid.ThingsAt(c).ToList<Thing>())
                        {
                            if (thing?.def?.destroyable != null)
                            {
                                if (GenSpawn.SpawningWipes(thingDef, thing.def))
                                {
                                    thing.Destroy(mode);
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }
	}
}
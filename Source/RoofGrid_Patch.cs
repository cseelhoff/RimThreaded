using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimThreaded
{
    class RoofGrid_Patch
    {
        internal static void RunDestructivePatches()
        {
            Type original = typeof(RoofGrid);
            Type patched = typeof(RoofGrid_Patch);

            RimThreadedHarmony.Prefix(original, patched, nameof(SetRoof));
        }
        public static bool SetRoof(RoofGrid __instance, IntVec3 c, RoofDef def)
        {
            Map map = __instance.map;
            int mcc = map.cellIndices.CellToIndex(c);
            if (__instance.roofGrid[mcc] != def)
            {
                __instance.roofGrid[mcc] = def;
                map.glowGrid.MarkGlowGridDirty(c);
                Room room = map.regionGrid.GetValidRegionAt_NoRebuild(c)?.Room;
                if(room != null)
                    room.Notify_RoofChanged();
                if (__instance.drawerInt != null)
                {
                    __instance.drawerInt.SetDirty();
                }

                map.mapDrawer.MapMeshDirty(c, MapMeshFlag.Roofs);
            }
            return false;
        }
	}
}

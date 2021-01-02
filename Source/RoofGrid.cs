using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{
    class RoofGrid_Patch
    {
        public static FieldRef<RoofGrid, RoofDef[]> roofGrid = FieldRefAccess<RoofGrid, RoofDef[]>("roofGrid");
        public static FieldRef<RoofGrid, Map> map = FieldRefAccess<RoofGrid, Map>("map");
        public static FieldRef<RoofGrid, CellBoolDrawer> drawerInt = FieldRefAccess<RoofGrid, CellBoolDrawer>("drawerInt");

        public static bool SetRoof(RoofGrid __instance, IntVec3 c, RoofDef def)
        {            
            if (roofGrid(__instance)[map(__instance).cellIndices.CellToIndex(c)] != def)
            {
                roofGrid(__instance)[map(__instance).cellIndices.CellToIndex(c)] = def;
                map(__instance).glowGrid.MarkGlowGridDirty(c);
                Region validRegionAt_NoRebuild = map(__instance).regionGrid.GetValidRegionAt_NoRebuild(c);
                if (validRegionAt_NoRebuild != null && validRegionAt_NoRebuild.Room != null)
                {
                    validRegionAt_NoRebuild.Room.Notify_RoofChanged();
                }
                if (drawerInt(__instance) != null)
                {
                    drawerInt(__instance).SetDirty();
                }

                map(__instance).mapDrawer.MapMeshDirty(c, MapMeshFlag.Roofs);
            }
            return false;
        }
    }
}

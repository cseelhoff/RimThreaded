using System;
using System.Collections.Generic;
using Verse;

namespace RimThreaded.RW_Patches
{
    class GridsUtility_Patch
    {
        internal static void RunDestructivePatches()
        {
            Type original = typeof(GridsUtility);
            Type patched = typeof(GridsUtility_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(Fogged), new Type[] { typeof(Thing) });
            RimThreadedHarmony.Prefix(original, patched, nameof(Fogged), new Type[] { typeof(IntVec3), typeof(Map) });
            RimThreadedHarmony.Prefix(original, patched, nameof(GetThingList));
            RimThreadedHarmony.Prefix(original, patched, nameof(GetEdifice));
        }

        public static bool GetEdifice(ref Building __result, IntVec3 c, Map map)
        {
            if (map == null)
            {
                __result = null;
                return false;
            }
            __result = map.edificeGrid[c];
            return false;
        }

        public static bool GetThingList(ref List<Thing> __result, IntVec3 c, Map map)
        {
            __result = null;
            if (map == null)
                return false;
            ThingGrid thingGrid = map.thingGrid;
            if (thingGrid == null)
                return false;
            __result = thingGrid.ThingsListAt(c);
            return false;
        }
        /*public static bool Fogged(this IntVec3 c, Map map)
        {
            return map.fogGrid.IsFogged(c);
        }*/
        public static bool Fogged(ref bool __result, IntVec3 c, Map map)
        {
            __result = false;
            if (c == null)
                return false;
            if (map == null)
                return false;
            FogGrid fogGrid = map.fogGrid;
            if (fogGrid == null)
                return false;
            __result = fogGrid.IsFogged(c);
            return false;
        }

        public static bool Fogged(ref bool __result, Thing t)
        {
            __result = false;
            if (t == null)
                return false;
            Map map = t.Map;
            if (map == null)
                return false;
            FogGrid fogGrid = map.fogGrid;
            if (fogGrid == null)
                return false;
            __result = fogGrid.IsFogged(t.Position);
            return false;
        }
    }

}

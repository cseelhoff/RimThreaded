using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{
    public class ThingGrid_Patch
    {
        public static FieldRef<ThingGrid, Map> map = FieldRefAccess<ThingGrid, Map>("map");
        public static FieldRef<ThingGrid, List<Thing>[]> thingGrid = FieldRefAccess<ThingGrid, List<Thing>[]>("thingGrid");

        public static void RunDestructivePatches()
        {
            Type original = typeof(ThingGrid);
            Type patched = typeof(ThingGrid_Patch);
            RimThreadedHarmony.Prefix(original, patched, "RegisterInCell");
            RimThreadedHarmony.Prefix(original, patched, "DeregisterInCell");
        }

        public static bool RegisterInCell(ThingGrid __instance, Thing t, IntVec3 c)
        {
            Map this_map = map(__instance);
            if (!c.InBounds(this_map))
            {
                Log.Warning(t.ToString() + " tried to register out of bounds at " + c + ". Destroying.", false);
                t.Destroy(DestroyMode.Vanish);
            }
            else
            {
                int index = this_map.cellIndices.CellToIndex(c);
                lock (__instance)
                {
                    thingGrid(__instance)[index].Add(t);
                }
            }
            return false;
        }

        public static bool DeregisterInCell(ThingGrid __instance, Thing t, IntVec3 c)
        {
            Map this_map = map(__instance);
            if (!c.InBounds(this_map))
            {
                Log.Error(t.ToString() + " tried to de-register out of bounds at " + c, false);
                return false;
            }

            int index = this_map.cellIndices.CellToIndex(c);
            List<Thing>[] thingGridInstance = thingGrid(__instance);
            List<Thing> thingList = thingGridInstance[index];
            if (thingList.Contains(t))
            {
                lock (__instance)
                {
                    thingList = thingGridInstance[index];
                    if (thingList.Contains(t))
                    {
                        List<Thing> newThingList = new List<Thing>(thingList);
                        newThingList.Remove(t);
                        thingGridInstance[index] = newThingList;
                    }
                }
            }

            return false;
        }

    }

}

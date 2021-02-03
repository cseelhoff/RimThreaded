using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Threading;
using Verse;

namespace RimThreaded
{
    public class ThingGrid_Patch
    {
        public static readonly List<Thing> EmptyThingList = new List<Thing>();
        public static AccessTools.FieldRef<ThingGrid, Map> map = AccessTools.FieldRefAccess<ThingGrid, Map>("map");
        public static AccessTools.FieldRef<ThingGrid, List<Thing>[]> thingGrid = AccessTools.FieldRefAccess<ThingGrid, List<Thing>[]>("thingGrid");

        public static bool RegisterInCell(ThingGrid __instance, Thing t, IntVec3 c)
        {
            Map this_map = map(__instance);
            CellIndices cellIndices = this_map.cellIndices;
            if (!c.InBounds(this_map))
            {
                Log.Warning(t.ToString() + " tried to register out of bounds at " + c + ". Destroying.", false);
                t.Destroy(DestroyMode.Vanish);
            }
            else
            {
                int index = cellIndices.CellToIndex(c);
                List<Thing> thingList = thingGrid(__instance)[index];
                lock (thingList) {
                    thingList.Add(t);
                }
            }
            return false;
        }

        public static bool DeregisterInCell(ThingGrid __instance, Thing t, IntVec3 c)
        {
            Map this_map = map(__instance);
            CellIndices cellIndices = this_map.cellIndices;
            if (!c.InBounds(this_map))
            {
                Log.Error(t.ToString() + " tried to de-register out of bounds at " + c, false);
            }
            else
            {
                int index = cellIndices.CellToIndex(c);
                List<Thing> thingList = thingGrid(__instance)[index];
                for (int i = 0; i < thingList.Count; i++)
                {
                    Thing thing;
                    try
                    {
                        thing = thingList[i];
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        break;
                    }
                    if (thing == t)
                    {
                        lock (thingList)
                        {
                            try
                            {
                                thing = thingList[i];
                                if (thing == t)
                                {
                                    thingList.RemoveAt(i);
                                    break;
                                }
                            }
                            catch (ArgumentOutOfRangeException) { }
                            for (int j = 0; j < thingList.Count; j++)
                            {
                                if (thingList[j] == t)
                                {
                                    thingList.RemoveAt(j);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }
        
        private static IEnumerable<Thing> ThingsAtEnumerableThing(ThingGrid __instance, IntVec3 c)
        {
            if (!c.InBounds(map(__instance)))
                yield break;
            List<Thing> list;
            try
            {
                list = thingGrid(__instance)[map(__instance).cellIndices.CellToIndex(c)];
            } catch (IndexOutOfRangeException) { yield break; }
            Thing thing;
            for (int i = 0; i < list.Count; i++)
            {
                try
                {
                    thing = list[i];
                }
                catch (ArgumentOutOfRangeException) { break; }
                yield return thing;
            }
        }
        public static bool ThingsAt(ThingGrid __instance, ref IEnumerable<Thing> __result, IntVec3 c)
        {
            __result = ThingsAtEnumerableThing(__instance, c);
            return false;
        }
        public static bool ThingAt(ThingGrid __instance, ref Thing __result, IntVec3 c, ThingCategory cat)
        {
            Map this_map = map(__instance);
            CellIndices cellIndices = this_map.cellIndices;
            if (!c.InBounds(this_map))
            {
                __result = null;
                return false;
            }

            List<Thing> thingList = thingGrid(__instance)[cellIndices.CellToIndex(c)];
            Thing thing;
            for (int index = 0; index < thingList.Count; index++)
            {
                try
                {
                    thing = thingList[index];
                }
                catch (ArgumentOutOfRangeException) { break; }
                if (thing.def.category == cat)
                {
                    __result = thing;
                    return false;
                }
            }
            __result = null;
            return false;
        }
        public static bool ThingAt(ThingGrid __instance, ref Thing __result, IntVec3 c, ThingDef def)
        {
            Map this_map = map(__instance);
            CellIndices cellIndices = this_map.cellIndices;
            if (!c.InBounds(this_map))
            {
                __result = null;
                return false;
            }
            List<Thing> thingList = thingGrid(__instance)[cellIndices.CellToIndex(c)];
            Thing thing;
            for (int index = 0; index < thingList.Count; index++)
            {
                try
                {
                    thing = thingList[index];
                }
                catch (ArgumentOutOfRangeException) { break; }
                if (thing.def == def)
                {
                    __result = thing;
                    return false;
                }
            }
            __result = null;
            return false;
        }

        public static bool ThingAt_Building_Door (ThingGrid __instance, ref Building_Door __result, IntVec3 c)
        {
            if (!c.InBounds(map(__instance)))
            {
                __result = null;
                return false;
            }

            List<Thing> thingList = thingGrid(__instance)[map(__instance).cellIndices.CellToIndex(c)];
            Thing thing;
            for (int index = 0; index < thingList.Count; index++)
            {
                try
                {
                    thing = thingList[index];
                } catch (ArgumentOutOfRangeException) { break; }
                Building_Door building_Door = thing as Building_Door;
                if (building_Door != null)
                {
                    __result = building_Door;
                    return false;
                }
            }
            __result = null;
            return false;
        }
    }

}

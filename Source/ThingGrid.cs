using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{
    public class ThingGrid_Patch
    {
        public static FieldRef<ThingGrid, Map> map = FieldRefAccess<ThingGrid, Map>("map");
        public static FieldRef<ThingGrid, List<Thing>[]> thingGrid = FieldRefAccess<ThingGrid, List<Thing>[]>("thingGrid");
        public static Dictionary<Map, Dictionary<WorkGiver_Scanner, Dictionary<float, List<HashSet<Thing>[]>>>> mapIngredientDict = new Dictionary<Map, Dictionary<WorkGiver_Scanner, Dictionary<float, List<HashSet<Thing>[]>>>>();
        public static Dictionary<ThingDef, Dictionary<WorkGiver_Scanner, float>> thingBillPoints = new Dictionary<ThingDef, Dictionary<WorkGiver_Scanner, float>>();
        public static int[] power2array = new int[] { 2, 4, 8, 16, 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192, 16384 }; // a 16000x16000 map is probably too big

        public static void RunDestructivePatches()
        {
            Type original = typeof(ThingGrid);
            Type patched = typeof(ThingGrid_Patch);
            RimThreadedHarmony.Prefix(original, patched, "RegisterInCell");
            RimThreadedHarmony.Prefix(original, patched, "DeregisterInCell");
        }
        public static int CellToIndexCustom(IntVec3 c, int mapSizeX, int cellSize)
        {
            return (c.z * mapSizeX + c.x) / cellSize;
        }
        public static int NumGridCellsCustom(int mapSizeX, int mapSizeZ, int cellSize)
        {
            return Mathf.CeilToInt((mapSizeX * mapSizeZ) / (float)cellSize);
        }
        public static bool RegisterInCell(ThingGrid __instance, Thing t, IntVec3 c)
        {
            Map this_map = map(__instance);
            int i;
            if (!c.InBounds(this_map))
            {
                Log.Warning(t.ToString() + " tried to register out of bounds at " + c + ". Destroying.", false);
                t.Destroy(DestroyMode.Vanish);
            }
            else
            {
                int mapSizeX = this_map.Size.x;
                int mapSizeZ = this_map.Size.z;

                int index = this_map.cellIndices.CellToIndex(c);
                Dictionary<WorkGiver_Scanner, float> billPointsDict = thingBillPoints[t.def];
                Dictionary<WorkGiver_Scanner, Dictionary<float, List<HashSet<Thing>[]>>> ingredientDict = mapIngredientDict[this_map];
                /*        
                    if (!uniqueBillDict.Value.TryGetValue(points, out List<HashSet<Thing>[]> jumboCellsList))
                    {
                        jumboCellsList = new List<HashSet<Thing>[]>();
                        i = 0;
                        while (true)
                        {
                            int power2 = power2array[i];
                            jumboCellsList.Add(new HashSet<Thing>[NumGridCellsCustom(mapSizeX, mapSizeZ, power2)]);
                            if (power2 >= this_map.Size.x && power2 >= this_map.Size.z)
                            {
                                break;
                            }
                        }
                        uniqueBillDict.Value.Add(points, jumboCellsList);
                    }
                    ingredientDict[billPoints.Key][billPoints.Value];
                */
                lock (__instance)
                {
                    thingGrid(__instance)[index].Add(t);
                    foreach (KeyValuePair<WorkGiver_Scanner, float> billPoints in billPointsDict)
                    {
                        i = 0;
                        int power2;
                        do
                        {
                            power2 = power2array[i];
                            ingredientDict[billPoints.Key][billPoints.Value][i][CellToIndexCustom(c, mapSizeX, power2)].Add(t);
                            i++;
                        } while (power2 < mapSizeX || power2 < mapSizeZ);
                    }                   
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

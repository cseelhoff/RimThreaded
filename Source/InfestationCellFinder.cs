using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using UnityEngine;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{

    public class InfestationCellFinder_Patch
    {
        public static ByteGrid distToColonyBuildingField = StaticFieldRefAccess<ByteGrid>(typeof(InfestationCellFinder), "distToColonyBuilding");
        public static ByteGrid closedAreaSizeField = StaticFieldRefAccess<ByteGrid>(typeof(InfestationCellFinder), "closedAreaSize");
        public static Dictionary<Region, float> regionsDistanceToUnroofedField = 
            StaticFieldRefAccess<Dictionary<Region, float>>(typeof(InfestationCellFinder), "regionsDistanceToUnroofed");
        public static bool CalculateDistanceToColonyBuildingGrid(Map map)
        {
            List<IntVec3> tmpColonyBuildingsLocs = new List<IntVec3>();
            List<KeyValuePair<IntVec3, float>> tmpDistanceResult = new List<KeyValuePair<IntVec3, float>>();
            ByteGrid distToColonyBuilding = new ByteGrid(map);            

            distToColonyBuilding.Clear(byte.MaxValue);
            tmpColonyBuildingsLocs.Clear();
            List<Building> allBuildingsColonist = map.listerBuildings.allBuildingsColonist;
            for (int i = 0; i < allBuildingsColonist.Count; i++)
            {
                tmpColonyBuildingsLocs.Add(allBuildingsColonist[i].Position);
            }

            Dijkstra<IntVec3>.Run(tmpColonyBuildingsLocs, (IntVec3 x) => DijkstraUtility.AdjacentCellsNeighborsGetter(x, map), (IntVec3 a, IntVec3 b) => (a.x == b.x || a.z == b.z) ? 1f : 1.41421354f, tmpDistanceResult);
            for (int j = 0; j < tmpDistanceResult.Count; j++)
            {
                distToColonyBuilding[tmpDistanceResult[j].Key] = (byte)Mathf.Min(tmpDistanceResult[j].Value, 254.999f);
            }
            distToColonyBuildingField = distToColonyBuilding;
            return false;
        }
        public static bool GetScoreAt(ref float __result, IntVec3 cell, Map map)
        {
            if ((float)(int)distToColonyBuildingField[cell] > 30f)
            {
                __result = 0f;
                return false;
            }

            if (!cell.Walkable(map))
            {
                __result = 0f;
                return false;
            }

            if (cell.Fogged(map))
            {
                __result = 0f;
                return false;
            }

            if (CellHasBlockingThings(cell, map))
            {
                __result = 0f;
                return false;
            }

            if (!cell.Roofed(map) || !cell.GetRoof(map).isThickRoof)
            {
                __result = 0f;
                return false;
            }

            Region region = cell.GetRegion(map);
            if (region == null)
            {
                __result = 0f;
                return false;
            }

            if (closedAreaSizeField[cell] < 2)
            {
                __result = 0f;
                return false;
            }

            float temperature = cell.GetTemperature(map);
            if (temperature < -17f)
            {
                __result = 0f;
                return false;
            }

            float mountainousnessScoreAt = GetMountainousnessScoreAt(cell, map);
            if (mountainousnessScoreAt < 0.17f)
            {
                __result = 0f;
                return false;
            }

            int num = StraightLineDistToUnroofed(cell, map);
            float value = regionsDistanceToUnroofedField.TryGetValue(region, out value) ? Mathf.Min(value, (float)num * 4f) : ((float)num * 1.15f);
            value = Mathf.Pow(value, 1.55f);
            float num2 = Mathf.InverseLerp(0f, 12f, num);
            float num3 = Mathf.Lerp(1f, 0.18f, map.glowGrid.GameGlowAt(cell));
            float num4 = 1f - Mathf.Clamp(DistToBlocker(cell, map) / 11f, 0f, 0.6f);
            float num5 = Mathf.InverseLerp(-17f, -7f, temperature);
            float f = value * num2 * num4 * mountainousnessScoreAt * num3 * num5;
            f = Mathf.Pow(f, 1.2f);
            if (f < 7.5f)
            {
                __result = 0f;
                return false;
            }

            __result = f;
            return false;
        }
        private static float DistToBlocker(IntVec3 cell, Map map)
        {
            int num = int.MinValue;
            int num2 = int.MinValue;
            for (int i = 0; i < 4; i++)
            {
                int num3 = 0;
                IntVec3 facingCell = new Rot4(i).FacingCell;
                int num4 = 0;
                while (true)
                {
                    IntVec3 c = cell + facingCell * num4;
                    num3 = num4;
                    if (!c.InBounds(map) || !c.Walkable(map))
                    {
                        break;
                    }

                    num4++;
                }

                if (num3 > num)
                {
                    num2 = num;
                    num = num3;
                }
                else if (num3 > num2)
                {
                    num2 = num3;
                }
            }

            return Mathf.Min(num, num2);
        }


        private static bool NoRoofAroundAndWalkable(IntVec3 cell, Map map)
        {
            if (!cell.Walkable(map))
            {
                return false;
            }

            if (cell.Roofed(map))
            {
                return false;
            }

            for (int i = 0; i < 4; i++)
            {
                IntVec3 c = new Rot4(i).FacingCell + cell;
                if (c.InBounds(map) && c.Roofed(map))
                {
                    return false;
                }
            }

            return true;
        }


        private static int StraightLineDistToUnroofed(IntVec3 cell, Map map)
        {
            int num = int.MaxValue;
            for (int i = 0; i < 4; i++)
            {
                int num2 = 0;
                IntVec3 facingCell = new Rot4(i).FacingCell;
                int num3 = 0;
                while (true)
                {
                    IntVec3 intVec = cell + facingCell * num3;
                    if (!intVec.InBounds(map))
                    {
                        num2 = int.MaxValue;
                        break;
                    }

                    num2 = num3;
                    if (NoRoofAroundAndWalkable(intVec, map))
                    {
                        break;
                    }

                    num3++;
                }

                if (num2 < num)
                {
                    num = num2;
                }
            }

            if (num == int.MaxValue)
            {
                return map.Size.x;
            }

            return num;
        }


        private static bool CellHasBlockingThings(IntVec3 cell, Map map)
        {
            List<Thing> thingList = cell.GetThingList(map);
            for (int i = 0; i < thingList.Count; i++)
            {
                if (thingList[i] is Pawn || thingList[i] is Hive || thingList[i] is TunnelHiveSpawner)
                {
                    return true;
                }

                if (thingList[i].def.category == ThingCategory.Building && thingList[i].def.passability == Traversability.Impassable && GenSpawn.SpawningWipes(ThingDefOf.Hive, thingList[i].def))
                {
                    return true;
                }
            }

            return false;
        }
        private static float GetMountainousnessScoreAt(IntVec3 cell, Map map)
        {
            float num = 0f;
            int num2 = 0;
            for (int i = 0; i < 700; i += 10)
            {
                IntVec3 c = cell + GenRadial.RadialPattern[i];
                if (c.InBounds(map))
                {
                    Building edifice = c.GetEdifice(map);
                    if (edifice != null && edifice.def.category == ThingCategory.Building && edifice.def.building.isNaturalRock)
                    {
                        num += 1f;
                    }
                    else if (c.Roofed(map) && c.GetRoof(map).isThickRoof)
                    {
                        num += 0.5f;
                    }

                    num2++;
                }
            }

            return num / (float)num2;
        }



    }
}

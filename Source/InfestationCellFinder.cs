using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{

    public class InfestationCellFinder_Patch
    {
        [ThreadStatic]
        static List<IntVec3> tmpColonyBuildingsLocs;

        [ThreadStatic]
        static List<KeyValuePair<IntVec3, float>> tmpDistanceResult;

        [ThreadStatic]
        private static ByteGrid distToColonyBuilding;

        public static ByteGrid distToColonyBuildingField = StaticFieldRefAccess<ByteGrid>(typeof(InfestationCellFinder), "distToColonyBuilding");

        public static bool CalculateDistanceToColonyBuildingGrid(Map map)
        {

            if (distToColonyBuilding == null)
            {
                distToColonyBuilding = new ByteGrid(map);
            }
            else if (!distToColonyBuilding.MapSizeMatches(map))
            {
                distToColonyBuilding.ClearAndResizeTo(map);
            }

            distToColonyBuilding.Clear(byte.MaxValue);


            if (tmpColonyBuildingsLocs == null)
            {
                List<IntVec3> tmpColonyBuildingsLocs = new List<IntVec3>();
            }
            else
            {
                tmpColonyBuildingsLocs.Clear();
            }

            List<Building> allBuildingsColonist = map.listerBuildings.allBuildingsColonist;
            for (int i = 0; i < allBuildingsColonist.Count; i++)
            {
                tmpColonyBuildingsLocs.Add(allBuildingsColonist[i].Position);
            }

            if (tmpDistanceResult == null)
            {
                tmpDistanceResult = new List<KeyValuePair<IntVec3, float>>();
            }
            else
            {
                tmpDistanceResult.Clear();
            }
            Dijkstra<IntVec3>.Run(tmpColonyBuildingsLocs, (IntVec3 x) => DijkstraUtility.AdjacentCellsNeighborsGetter(x, map), (IntVec3 a, IntVec3 b) => (a.x == b.x || a.z == b.z) ? 1f : 1.41421354f, tmpDistanceResult);
            for (int j = 0; j < tmpDistanceResult.Count; j++)
            {
                distToColonyBuilding[tmpDistanceResult[j].Key] = (byte)Mathf.Min(tmpDistanceResult[j].Value, 254.999f);
            }
            distToColonyBuildingField = distToColonyBuilding;
            return false;
        }
        
    }
}

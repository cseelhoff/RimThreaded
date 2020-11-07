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

namespace RimThreaded
{

    public class InfestationCellFinder_Patch
    {
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
            return false;
        }

    }
}

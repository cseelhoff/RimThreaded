using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;
using System.Linq;

namespace RimThreaded
{

    public class CellFinder_Patch
    {
        [ThreadStatic] public static List<IntVec3> workingCells;
        [ThreadStatic] public static List<Region> workingRegions;
        [ThreadStatic] public static List<int> workingListX;
        [ThreadStatic] public static List<int> workingListZ;
        [ThreadStatic] public static Dictionary<IntVec3, float> tmpDistances;
        [ThreadStatic] public static Dictionary<IntVec3, IntVec3> tmpParents;
        [ThreadStatic] public static List<IntVec3> tmpCells;
        [ThreadStatic] public static List<Thing> tmpUniqueWipedThings;
        [ThreadStatic] public static List<IntVec3> mapEdgeCells;
        [ThreadStatic] public static IntVec3 mapEdgeCellsSize;
        [ThreadStatic] public static List<IntVec3>[] mapSingleEdgeCells;
        [ThreadStatic] public static IntVec3 mapSingleEdgeCellsSize;

        public static void InitializeThreadStatics()
        {
            workingCells = new List<IntVec3>();
            workingListX = new List<int>();
            workingListZ = new List<int>();
            tmpDistances = new Dictionary<IntVec3, float>();
            tmpParents = new Dictionary<IntVec3, IntVec3>();
            workingRegions = new List<Region>();
            tmpCells = new List<IntVec3>();
            tmpUniqueWipedThings = new List<Thing>();
        }

    }
}

using HarmonyLib;
using System;
using System.Collections.Generic;
using Verse;

namespace RimThreaded
{

    public class FloodFiller_Patch
    {

        [ThreadStatic] public static AccessTools.FieldRef<FloodFiller, bool> working;
        [ThreadStatic] public static AccessTools.FieldRef<FloodFiller, List<int>> visited;
        [ThreadStatic] public static AccessTools.FieldRef<FloodFiller, CellGrid> parentGrid;
        [ThreadStatic] public static AccessTools.FieldRef<FloodFiller, Map> map;
        [ThreadStatic] public static AccessTools.FieldRef<FloodFiller, Queue<IntVec3>> openSet;
        [ThreadStatic] public static AccessTools.FieldRef<FloodFiller, IntGrid> traversalDistance;

        public static void InitializeThreadStatics()
        {
            working = AccessTools.FieldRefAccess<FloodFiller, bool>("working");
            visited = AccessTools.FieldRefAccess<FloodFiller, List<int>>("visited"); 
            parentGrid = AccessTools.FieldRefAccess<FloodFiller, CellGrid>("parentGrid");
            map = AccessTools.FieldRefAccess<FloodFiller, Map>("map");
            openSet = AccessTools.FieldRefAccess<FloodFiller, Queue<IntVec3>>("openSet");
            traversalDistance = AccessTools.FieldRefAccess<FloodFiller, IntGrid>("traversalDistance");
        }

        internal static void RunNonDestructivePatches()
        {
            Type original = typeof(FloodFiller);
            Type patched = typeof(FloodFiller_Patch);
            RimThreadedHarmony.AddAllMatchingFields(original, patched);
            RimThreadedHarmony.TranspileFieldReplacements(original, "FloodFill");
            RimThreadedHarmony.TranspileFieldReplacements(original, "ClearVisited");
        }

    }
}
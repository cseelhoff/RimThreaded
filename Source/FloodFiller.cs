using HarmonyLib;
using System;
using System.Collections.Generic;
using Verse;

namespace RimThreaded
{

    public class FloodFiller_Patch
    {

        [ThreadStatic] public static bool working;
        [ThreadStatic] public static List<int> visited;
        [ThreadStatic] public static CellGrid parentGrid;
        [ThreadStatic] public static Map map;
        [ThreadStatic] public static Queue<IntVec3> openSet;
        [ThreadStatic] public static IntGrid traversalDistance;

        public static void InitializeThreadStatics()
        {
            visited = new List<int>();
            openSet = new Queue<IntVec3>();
        }

        internal static void RunNonDestructivePatches()
        {
            Type original = typeof(FloodFiller);
            Type patched = typeof(FloodFiller_Patch);
            RimThreadedHarmony.AddAllMatchingFields(original, patched);
            RimThreadedHarmony.TranspileFieldReplacements(original, "FloodFill", new Type[] { typeof(IntVec3), typeof(Predicate<IntVec3>), typeof(Func<IntVec3, int, bool>), typeof(int), typeof(bool), typeof(IEnumerable<IntVec3>) });
            RimThreadedHarmony.TranspileFieldReplacements(original, "ClearVisited");
        }

    }
}
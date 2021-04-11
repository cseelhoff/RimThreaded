using System;
using System.Collections.Generic;
using Verse;

namespace RimThreaded
{

    public class WorldFloodFiller_Patch
    {
        [ThreadStatic] public static Queue<int> openSet;
        [ThreadStatic] public static List<int> traversalDistance;
        [ThreadStatic] public static List<int> visited;
        [ThreadStatic] public static bool working;
        public static void InitializeThreads()
        {
            openSet = new Queue<int>();
            traversalDistance = new List<int>();
            visited = new List<int>();
        }

        internal static void RunNonDestructivePatches()
        {
            Type original = typeof(WorldFloodFiller);
            Type patched = typeof(WorldFloodFiller_Patch);

            RimThreadedHarmony.AddAllMatchingFields(original, patched);
            RimThreadedHarmony.TranspileFieldReplacements(original, "FloodFill", new Type[] { typeof(int), typeof(Predicate<int>), typeof(Func<int, int, bool>), typeof(int), typeof(IEnumerable<int>) });

        }
    }
}

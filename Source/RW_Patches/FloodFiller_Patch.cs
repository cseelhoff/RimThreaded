using System;
using System.Collections.Generic;
using Verse;

namespace RimThreaded.RW_Patches
{

    public class FloodFiller_Patch
    {
        internal static void RunDestructivePatches()
        {
            Type original = typeof(FloodFiller);
            Type patched = typeof(FloodFiller_Patch);
            RimThreadedHarmony.Prefix(original, patched, "FloodFill", new[] { typeof(IntVec3), typeof(Predicate<IntVec3>), typeof(Func<IntVec3, int, bool>), typeof(int), typeof(bool), typeof(IEnumerable<IntVec3>) });
        }

        public static bool FloodFill(FloodFiller __instance,
              IntVec3 root,
              Predicate<IntVec3> passCheck,
              Func<IntVec3, int, bool> processor,
              int maxCellsToProcess = 2147483647,
              bool rememberParents = false,
              IEnumerable<IntVec3> extraRoots = null)
        {
            lock (__instance)
            {
                if (__instance.working)
                    Log.Error("Nested FloodFill calls are not allowed. This will cause bugs.");
                __instance.working = true;
                __instance.ClearVisited();
                if (rememberParents && __instance.parentGrid == null)
                    __instance.parentGrid = new CellGrid(__instance.map);
                if (root.IsValid && extraRoots == null && !passCheck(root))
                {
                    if (rememberParents)
                        __instance.parentGrid[root] = IntVec3.Invalid;
                    __instance.working = false;
                }
                else
                {
                    int area = __instance.map.Area;
                    IntVec3[] directionsAround = GenAdj.CardinalDirectionsAround;
                    int length = directionsAround.Length;
                    CellIndices cellIndices = __instance.map.cellIndices;
                    int num1 = 0;
                    __instance.openSet.Clear();
                    if (root.IsValid)
                    {
                        int index = cellIndices.CellToIndex(root);
                        __instance.visited.Add(index);
                        __instance.traversalDistance[index] = 0;
                        __instance.openSet.Enqueue(root);
                    }
                    if (extraRoots != null)
                    {
                        if (extraRoots is IList<IntVec3> intVec3List)
                        {
                            for (int index1 = 0; index1 < intVec3List.Count; ++index1)
                            {
                                int index2 = cellIndices.CellToIndex(intVec3List[index1]);
                                __instance.visited.Add(index2);
                                __instance.traversalDistance[index2] = 0;
                                __instance.openSet.Enqueue(intVec3List[index1]);
                            }
                        }
                        else
                        {
                            foreach (IntVec3 extraRoot in extraRoots)
                            {
                                int index = cellIndices.CellToIndex(extraRoot);
                                __instance.visited.Add(index);
                                __instance.traversalDistance[index] = 0;
                                __instance.openSet.Enqueue(extraRoot);
                            }
                        }
                    }
                    if (rememberParents)
                    {
                        for (int index = 0; index < __instance.visited.Count; ++index)
                        {
                            IntVec3 cell = cellIndices.IndexToCell(__instance.visited[index]);
                            __instance.parentGrid[__instance.visited[index]] = passCheck(cell) ? cell : IntVec3.Invalid;
                        }
                    }
                    while (__instance.openSet.Count > 0)
                    {
                        IntVec3 c1 = __instance.openSet.Dequeue();
                        int num2 = __instance.traversalDistance[cellIndices.CellToIndex(c1)];
                        if (!processor(c1, num2))
                        {
                            ++num1;
                            if (num1 != maxCellsToProcess)
                            {
                                for (int index1 = 0; index1 < length; ++index1)
                                {
                                    IntVec3 c2 = c1 + directionsAround[index1];
                                    int index2 = cellIndices.CellToIndex(c2);
                                    if (!c2.InBounds(__instance.map) || __instance.traversalDistance[index2] != -1 ||
                                        !passCheck(c2)) continue;
                                    __instance.visited.Add(index2);
                                    __instance.openSet.Enqueue(c2);
                                    __instance.traversalDistance[index2] = num2 + 1;
                                    if (rememberParents)
                                        __instance.parentGrid[index2] = c1;
                                }

                                if (__instance.openSet.Count <= area) continue;
                                Log.Error("Overflow on flood fill (>" + area + " cells). Make sure we're not flooding over the same area after we check it.");
                                __instance.working = false;
                                return false;
                            }
                        }
                        break;
                    }
                    __instance.working = false;
                }
            }
            return false;
        }
    }
}
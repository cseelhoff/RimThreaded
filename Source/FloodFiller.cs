using HarmonyLib;
using System;
using System.Collections.Generic;
using Verse;

namespace RimThreaded
{

    public class FloodFiller_Patch
    {
        public static AccessTools.FieldRef<FloodFiller, bool> working =
            AccessTools.FieldRefAccess<FloodFiller, bool>("working");
        public static AccessTools.FieldRef<FloodFiller, List<int>> visited =
            AccessTools.FieldRefAccess<FloodFiller, List<int>>("visited");
        public static AccessTools.FieldRef<FloodFiller, CellGrid> parentGrid =
            AccessTools.FieldRefAccess<FloodFiller, CellGrid>("parentGrid");
        public static AccessTools.FieldRef<FloodFiller, Map> map =
            AccessTools.FieldRefAccess<FloodFiller, Map>("map");
        public static AccessTools.FieldRef<FloodFiller, Queue<IntVec3>> openSet =
            AccessTools.FieldRefAccess<FloodFiller, Queue<IntVec3>>("openSet");
        public static AccessTools.FieldRef<FloodFiller, IntGrid> traversalDistance =
            AccessTools.FieldRefAccess<FloodFiller, IntGrid>("traversalDistance");

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
                if (working(__instance))
                    Log.Error("Nested FloodFill calls are not allowed. This will cause bugs.", false);
                working(__instance) = true;
                ClearVisited(__instance);
                if (rememberParents && parentGrid(__instance) == null)
                    parentGrid(__instance) = new CellGrid(map(__instance));
                if (root.IsValid && extraRoots == null && !passCheck(root))
                {
                    if (rememberParents)
                        parentGrid(__instance)[root] = IntVec3.Invalid;
                    working(__instance) = false;
                }
                else
                {
                    int area = map(__instance).Area;
                    IntVec3[] directionsAround = GenAdj.CardinalDirectionsAround;
                    int length = directionsAround.Length;
                    CellIndices cellIndices = map(__instance).cellIndices;
                    int num1 = 0;
                    openSet(__instance).Clear();
                    if (root.IsValid)
                    {
                        int index = cellIndices.CellToIndex(root);
                        visited(__instance).Add(index);
                        traversalDistance(__instance)[index] = 0;
                        openSet(__instance).Enqueue(root);
                    }
                    if (extraRoots != null)
                    {
                        if (extraRoots is IList<IntVec3> intVec3List)
                        {
                            for (int index1 = 0; index1 < intVec3List.Count; ++index1)
                            {
                                int index2 = cellIndices.CellToIndex(intVec3List[index1]);
                                visited(__instance).Add(index2);
                                traversalDistance(__instance)[index2] = 0;
                                openSet(__instance).Enqueue(intVec3List[index1]);
                            }
                        }
                        else
                        {
                            foreach (IntVec3 extraRoot in extraRoots)
                            {
                                int index = cellIndices.CellToIndex(extraRoot);
                                visited(__instance).Add(index);
                                traversalDistance(__instance)[index] = 0;
                                openSet(__instance).Enqueue(extraRoot);
                            }
                        }
                    }
                    if (rememberParents)
                    {
                        for (int index = 0; index < visited(__instance).Count; ++index)
                        {
                            IntVec3 cell = cellIndices.IndexToCell(visited(__instance)[index]);
                            parentGrid(__instance)[visited(__instance)[index]] = passCheck(cell) ? cell : IntVec3.Invalid;
                        }
                    }
                    while (openSet(__instance).Count > 0)
                    {
                        IntVec3 c1 = openSet(__instance).Dequeue();
                        int num2 = traversalDistance(__instance)[cellIndices.CellToIndex(c1)];
                        if (!processor(c1, num2))
                        {
                            ++num1;
                            if (num1 != maxCellsToProcess)
                            {
                                for (int index1 = 0; index1 < length; ++index1)
                                {
                                    IntVec3 c2 = c1 + directionsAround[index1];
                                    int index2 = cellIndices.CellToIndex(c2);
                                    if (c2.InBounds(map(__instance)) && traversalDistance(__instance)[index2] == -1 && passCheck(c2))
                                    {
                                        visited(__instance).Add(index2);
                                        openSet(__instance).Enqueue(c2);
                                        traversalDistance(__instance)[index2] = num2 + 1;
                                        if (rememberParents)
                                            parentGrid(__instance)[index2] = c1;
                                    }
                                }
                                if (openSet(__instance).Count > area)
                                {
                                    Log.Error("Overflow on flood fill (>" + area + " cells). Make sure we're not flooding over the same area after we check it.", false);
                                    working(__instance) = false;
                                    return false;
                                }
                            }
                            else
                                break;
                        }
                        else
                            break;
                    }
                    working(__instance) = false;
                }
            }
            return false;
        }
        private static void ClearVisited(FloodFiller __instance)
        {
            int index1 = 0;
            for (int count = visited(__instance).Count; index1 < count; ++index1)
            {
                int index2 = visited(__instance)[index1];
                traversalDistance(__instance)[index2] = -1;
                if (parentGrid(__instance) != null)
                    parentGrid(__instance)[index2] = IntVec3.Invalid;
            }
            visited(__instance).Clear();
            openSet(__instance).Clear();
        }
    }
}
using System;
using System.Collections.Generic;
using Verse;
using RimWorld.Planet;

namespace RimThreaded
{

    public class WorldFloodFiller_Patch
    {
        [ThreadStatic]
        static Queue<int> openSet;
        [ThreadStatic]
        static List<int> traversalDistance;
        [ThreadStatic]
        static List<int> visited;
        [ThreadStatic]
        static bool working;

        public static bool FloodFill(WorldFloodFiller __instance, int rootTile, Predicate<int> passCheck, Func<int, int, bool> processor, int maxTilesToProcess = int.MaxValue, IEnumerable<int> extraRootTiles = null)
        {            
            if (openSet == null)
            {
                openSet = new Queue<int>();
            }
            if (traversalDistance == null)
            {
                 traversalDistance = new List<int>();
            }
            if (visited == null)
            {
                visited = new List<int>();
            }

            if (working)
            {
                Log.Error("Nested FloodFill calls are not allowed. This will cause bugs.");
            }

            working = true;
            int j = 0;
            for (int count = visited.Count; j < count; j++)
            {
                traversalDistance[visited[j]] = -1;
            }
            visited.Clear();
            openSet.Clear();
            if (rootTile != -1 && extraRootTiles == null && !passCheck(rootTile))
            {
                working = false;
                return false;
            }

            int tilesCount = Find.WorldGrid.TilesCount;
            if (traversalDistance.Count != tilesCount)
            {
                traversalDistance.Clear();
                for (int i = 0; i < tilesCount; i++)
                {
                    traversalDistance.Add(-1);
                }
            }

            WorldGrid worldGrid = Find.WorldGrid;
            List<int> tileIDToNeighbors_offsets = worldGrid.tileIDToNeighbors_offsets;
            List<int> tileIDToNeighbors_values = worldGrid.tileIDToNeighbors_values;
            openSet.Clear();
            if (rootTile != -1)
            {
                visited.Add(rootTile);
                traversalDistance[rootTile] = 0;
                openSet.Enqueue(rootTile);
            }

            if (extraRootTiles != null)
            {
                visited.AddRange(extraRootTiles);
                IList<int> list = extraRootTiles as IList<int>;
                if (list != null)
                {
                    int num3 = list[j];
                    traversalDistance[num3] = 0;
                    openSet.Enqueue(num3);
                }
                else
                {
                    foreach (int extraRootTile in extraRootTiles)
                    {
                        traversalDistance[extraRootTile] = 0;
                        openSet.Enqueue(extraRootTile);
                    }
                }
            }

            loop3(processor, maxTilesToProcess, tileIDToNeighbors_offsets, 
                tileIDToNeighbors_values, passCheck, tilesCount);

            working = false;
            return false;
        }

        private static void loop3(Func<int, int, bool> processor, int maxTilesToProcess, 
            List<int> tileIDToNeighbors_offsets, List<int> tileIDToNeighbors_values,
            Predicate<int> passCheck, int num)
        {
            int num2 = 0;
            while (openSet.Count > 0)
            {
                int num4 = openSet.Dequeue();
                int num5 = traversalDistance[num4];
                if (processor(num4, num5))
                {
                    break;
                }

                num2++;
                if (num2 == maxTilesToProcess)
                {
                    break;
                }

                int num6 = (num4 + 1 < tileIDToNeighbors_offsets.Count) ? tileIDToNeighbors_offsets[num4 + 1] : tileIDToNeighbors_values.Count;
                for (int k = tileIDToNeighbors_offsets[num4]; k < num6; k++)
                {
                    int num7 = tileIDToNeighbors_values[k];
                    if (traversalDistance[num7] == -1 && passCheck(num7))
                    {
                        visited.Add(num7);
                        openSet.Enqueue(num7);
                        traversalDistance[num7] = num5 + 1;
                    }
                }

                if (openSet.Count > num)
                {
                    Log.Error("Overflow on world flood fill (>" + num + " cells). Make sure we're not flooding over the same area after we check it.");
                    working = false;
                    return;
                }
            }
        }

    }
}

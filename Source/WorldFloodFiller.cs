using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using RimWorld.Planet;

namespace RimThreaded
{

    public class WorldFloodFiller_Patch
    {
        public static bool FloodFill(WorldFloodFiller __instance, int rootTile, Predicate<int> passCheck, Func<int, int, bool> processor, int maxTilesToProcess = int.MaxValue, IEnumerable<int> extraRootTiles = null)
        {
            bool working = false;
            Queue<int> openSet = new Queue<int>();    
            List<int> traversalDistance = new List<int>();
            List<int> visited = new List<int>();

            if (working)
            {
                Log.Error("Nested FloodFill calls are not allowed. This will cause bugs.");
            }

            working = true;
            //ClearVisited();
            if (rootTile != -1 && extraRootTiles == null && !passCheck(rootTile))
            {
                working = false;
                return false;
            }

            int tilesCount = Find.WorldGrid.TilesCount;
            int num = tilesCount;
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
            int num2 = 0;
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
                    for (int j = 0; j < list.Count; j++)
                    {
                        int num3 = list[j];
                        traversalDistance[num3] = 0;
                        openSet.Enqueue(num3);
                    }
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
                    return false;
                }
            }

            working = false;
            return false;
        }



    }
}

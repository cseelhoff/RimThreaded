using RimWorld.Planet;
using System;
using System.Collections.Generic;
using Verse;

namespace RimThreaded
{

    public class WorldGrid_Patch
    {
        [ThreadStatic]
        public static List<int> tmpNeighbors;            

        public static bool IsNeighbor(WorldGrid __instance, ref bool __result, int tile1, int tile2)
        {
            if (tmpNeighbors == null)
            {
                tmpNeighbors = new List<int>();
            }
            __instance.GetTileNeighbors(tile1, tmpNeighbors);
            __result = tmpNeighbors.Contains(tile2);
            return false;
        }
        public static bool GetNeighborId(WorldGrid __instance, ref int __result, int tile1, int tile2)
        {
            if (tmpNeighbors == null)
            {
                tmpNeighbors = new List<int>();
            }
            __instance.GetTileNeighbors(tile1, tmpNeighbors);
            __result = tmpNeighbors.IndexOf(tile2);
            return false;
        }
        public static bool GetTileNeighbor(WorldGrid __instance, ref int __result, int tileID, int adjacentId)
        {
            if (tmpNeighbors == null)
            {
                tmpNeighbors = new List<int>();
            }
            __instance.GetTileNeighbors(tileID, tmpNeighbors);
            __result = tmpNeighbors[adjacentId];
            return false;
        }

        public static bool FindMostReasonableAdjacentTileForDisplayedPathCost(WorldGrid __instance, ref int __result, int fromTile)
        {
            Tile tile = __instance.tiles[fromTile];
            float num = 1f;
            int num2 = -1;
            List<Tile.RoadLink> roads = tile.Roads;
            if (roads != null)
            {
                for (int i = 0; i < roads.Count; i++)
                {
                    float movementCostMultiplier = roads[i].road.movementCostMultiplier;
                    if (movementCostMultiplier < num && !Find.World.Impassable(roads[i].neighbor))
                    {
                        num = movementCostMultiplier;
                        num2 = roads[i].neighbor;
                    }
                }
            }

            if (num2 != -1)
            {
                __result = num2;
                return false;
            }
            if (tmpNeighbors == null)
            {
                tmpNeighbors = new List<int>();
            }
            else
            {
                tmpNeighbors.Clear();
            }
            __instance.GetTileNeighbors(fromTile, tmpNeighbors);
            for (int j = 0; j < tmpNeighbors.Count; j++)
            {
                if (!Find.World.Impassable(tmpNeighbors[j]))
                {
                    __result = tmpNeighbors[j];
                    return false;
                }
            }

            __result = fromTile;
            return false;
        }
    }
}

using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using System.Reflection;
using static HarmonyLib.AccessTools;
using UnityEngine;
using System.Linq;

namespace RimThreaded
{

    public class CellFinder_Patch
    {
        [ThreadStatic]
        private static List<IntVec3> workingCells;
        [ThreadStatic]
        private static List<Region> workingRegions;
        [ThreadStatic]
        private static List<int> workingListX;
        [ThreadStatic]
        private static List<int> workingListZ;
        [ThreadStatic]
        private static Dictionary<IntVec3, float> tmpDistances;
        [ThreadStatic]
        private static Dictionary<IntVec3, IntVec3> tmpParents;
        [ThreadStatic]
        private static List<IntVec3> tmpCells;
        [ThreadStatic]
        private static List<Thing> tmpUniqueWipedThings;
        [ThreadStatic]
        private static List<IntVec3> mapEdgeCells;
        [ThreadStatic]
        private static IntVec3 mapEdgeCellsSize;
        [ThreadStatic]
        private static List<IntVec3>[] mapSingleEdgeCells;
        [ThreadStatic]
        private static IntVec3 mapSingleEdgeCellsSize;

        public static bool TryFindRandomCellNear(ref bool __result, IntVec3 root, Map map, int squareRadius, Predicate<IntVec3> validator, out IntVec3 result, int maxTries = -1)
        {
            if (map == null || map.Size == null)
            {
                result = root;
                __result = false;
                return false;
            }

            int num = root.x - squareRadius;
            int num2 = root.x + squareRadius;
            int num3 = root.z - squareRadius;
            int num4 = root.z + squareRadius;
            int num5 = (num2 - num + 1) * (num4 - num3 + 1);
            if (num < 0)
            {
                num = 0;
            }

            if (num3 < 0)
            {
                num3 = 0;
            }

            if (num2 > map.Size.x)
            {
                num2 = map.Size.x;
            }

            if (num4 > map.Size.z)
            {
                num4 = map.Size.z;
            }

            int num6;
            bool flag;
            if (maxTries < 0 || maxTries >= num5)
            {
                num6 = 20;
                flag = false;
            }
            else
            {
                num6 = maxTries;
                flag = true;
            }

            for (int i = 0; i < num6; i++)
            {
                IntVec3 intVec = new IntVec3(Rand.RangeInclusive(num, num2), 0, Rand.RangeInclusive(num3, num4));
                if (validator == null || validator(intVec))
                {
                    if (DebugViewSettings.drawDestSearch)
                    {
                        map.debugDrawer.FlashCell(intVec, 0.5f, "found");
                    }

                    result = intVec;
                    __result = true;
                    return false;
                }

                if (DebugViewSettings.drawDestSearch)
                {
                    map.debugDrawer.FlashCell(intVec, 0f, "inv");
                }
            }

            if (flag)
            {
                result = root;
                __result = false;
                return false;
            }

            if (workingListX == null)
            {
                workingListX = new List<int>();
            }
            else
            {
                workingListX.Clear();
            }

            if (workingListZ == null)
            {
                workingListZ = new List<int>();
            }
            else
            {
                workingListZ.Clear();
            }

            for (int j = num; j <= num2; j++)
            {
                workingListX.Add(j);
            }

            for (int k = num3; k <= num4; k++)
            {
                workingListZ.Add(k);
            }

            workingListX.Shuffle();
            workingListZ.Shuffle();
            for (int l = 0; l < workingListX.Count; l++)
            {
                for (int m = 0; m < workingListZ.Count; m++)
                {
                    IntVec3 intVec = new IntVec3(workingListX[l], 0, workingListZ[m]);
                    if (validator(intVec))
                    {
                        if (DebugViewSettings.drawDestSearch)
                        {
                            map.debugDrawer.FlashCell(intVec, 0.6f, "found2");
                        }

                        result = intVec;
                        __result = true;
                        return false;
                    }

                    if (DebugViewSettings.drawDestSearch)
                    {
                        map.debugDrawer.FlashCell(intVec, 0.25f, "inv2");
                    }
                }
            }

            result = root;
            __result = false;
            return false;
        }

        public static bool TryFindRandomCellInRegion(ref bool __result, Region reg, Predicate<IntVec3> validator, out IntVec3 result)
		{
			for (int i = 0; i < 10; i++)
			{
				result = reg.RandomCell;
				if (validator == null || validator(result))
				{
					__result = true;
					return false;
				}
			}
            if (workingCells == null)
            {
                workingCells = new List<IntVec3>();
            } else
            {
                workingCells.Clear();
            }
			workingCells.AddRange(reg.Cells);
			workingCells.Shuffle();
			for (int j = 0; j < workingCells.Count; j++)
			{
				result = workingCells[j];
				if (validator == null || validator(result))
				{
					__result = true;
					return false;
				}
			}
			result = reg.RandomCell;
			__result = false;
			return false;
		}

        private static IEnumerable<IntVec3> GetAdjacentCardinalCellsForBestStandCell(IntVec3 x, float radius, Pawn pawn)
        {
            if ((x - pawn.Position).LengthManhattan > radius)
            {
                yield break;
            }

            for (int i = 0; i < 4; i++)
            {
                IntVec3 intVec = x + GenAdj.CardinalDirections[i];
                if (intVec.InBounds(pawn.Map) && intVec.Walkable(pawn.Map))
                {
                    Building_Door building_Door = intVec.GetEdifice(pawn.Map) as Building_Door;
                    if (building_Door == null || building_Door.CanPhysicallyPass(pawn))
                    {
                        yield return intVec;
                    }
                }
            }
        }
        public static bool TryFindBestPawnStandCell(ref bool __result, Pawn forPawn, out IntVec3 cell, bool cellByCell = false)
        {
            cell = IntVec3.Invalid;
            int num = -1;
            float radius = 10f;
            if(tmpDistances == null)
            {
                tmpDistances = new Dictionary<IntVec3, float>();
            }
            if(tmpParents == null)
            {
                tmpParents = new Dictionary<IntVec3, IntVec3>();
            }
            while (true)
            {
                tmpDistances.Clear();
                tmpParents.Clear();
                Dijkstra<IntVec3>.Run(forPawn.Position, (IntVec3 x) => GetAdjacentCardinalCellsForBestStandCell(x, radius, forPawn), delegate (IntVec3 from, IntVec3 to)
                {
                    float num4 = 1f;
                    if (from.x != to.x && from.z != to.z)
                    {
                        num4 = 1.41421354f;
                    }

                    if (!to.Standable(forPawn.Map))
                    {
                        num4 += 3f;
                    }

                    if (PawnUtility.AnyPawnBlockingPathAt(to, forPawn))
                    {
                        num4 = ((to.GetThingList(forPawn.Map).Find((Thing x) => x is Pawn && x.HostileTo(forPawn)) == null) ? (num4 + 15f) : (num4 + 40f));
                    }

                    Building_Door building_Door = to.GetEdifice(forPawn.Map) as Building_Door;
                    if (building_Door != null && !building_Door.FreePassage)
                    {
                        num4 = ((!building_Door.PawnCanOpen(forPawn)) ? (num4 + 50f) : (num4 + 6f));
                    }

                    return num4;
                }, tmpDistances, tmpParents);
                if (tmpDistances.Count == num)
                {
                    __result = false;
                    return false;
                }

                float num2 = 0f;
                foreach (KeyValuePair<IntVec3, float> tmpDistance in tmpDistances)
                {
                    if ((!cell.IsValid || !(tmpDistance.Value >= num2)) && tmpDistance.Key.Walkable(forPawn.Map) && !PawnUtility.AnyPawnBlockingPathAt(tmpDistance.Key, forPawn))
                    {
                        Building_Door door = tmpDistance.Key.GetDoor(forPawn.Map);
                        if (door == null || door.FreePassage)
                        {
                            cell = tmpDistance.Key;
                            num2 = tmpDistance.Value;
                        }
                    }
                }

                if (cell.IsValid)
                {
                    if (!cellByCell)
                    {
                        __result = true;
                        return false;
                    }

                    IntVec3 intVec = cell;
                    int num3 = 0;
                    while (intVec.IsValid && intVec != forPawn.Position)
                    {
                        num3++;
                        if (num3 >= 10000)
                        {
                            Log.Error("Too many iterations.");
                            break;
                        }

                        if (intVec.Walkable(forPawn.Map))
                        {
                            Building_Door door2 = intVec.GetDoor(forPawn.Map);
                            if (door2 == null || door2.FreePassage)
                            {
                                cell = intVec;
                            }
                        }

                        intVec = tmpParents[intVec];
                    }

                    __result = true;
                    return false;
                }

                if (radius > forPawn.Map.Size.x && radius > forPawn.Map.Size.z)
                {
                    break;
                }

                radius *= 2f;
                num = tmpDistances.Count;
            }

            __result = false;
            return false;
        }

        public static bool TryFindRandomReachableCellNear(ref bool __result,
          IntVec3 root,
          Map map,
          float radius,
          TraverseParms traverseParms,
          Predicate<IntVec3> cellValidator,
          Predicate<Region> regionValidator,
          out IntVec3 result,
          int maxRegions = 999999)
        {
            if (map == null)
            {
                Log.ErrorOnce("Tried to find reachable cell in a null map", 61037855);
                result = IntVec3.Invalid;
                return false;
            }

            Region region = root.GetRegion(map);
            if (region == null)
            {
                result = IntVec3.Invalid;
                return false;
            }
            if (workingRegions == null)
            {
                workingRegions = new List<Region>();
            }
            else
            {
                workingRegions.Clear();
            }
            float radSquared = radius * radius;
            RegionTraverser.BreadthFirstTraverse(region, (Region from, Region r) => r.Allows(traverseParms, isDestination: true) && (radius > 1000f || r.extentsClose.ClosestDistSquaredTo(root) <= radSquared) && (regionValidator == null || regionValidator(r)), delegate (Region r)
            {
                workingRegions.Add(r);
                return false;
            }, maxRegions);
            while (workingRegions.Count > 0)
            {
                Region region2 = workingRegions.RandomElementByWeight((Region r) => r.CellCount);
                if (region2.TryFindRandomCellInRegion((IntVec3 c) => (c - root).LengthHorizontalSquared <= radSquared && (cellValidator == null || cellValidator(c)), out result))
                {
                    //workingRegions.Clear();
                    return true;
                }

                workingRegions.Remove(region2);
            }

            result = IntVec3.Invalid;
            //workingRegions.Clear();
            return false;
        }

        public static bool TryFindRandomCellInsideWith(ref bool __result, CellRect cellRect, Predicate<IntVec3> predicate, out IntVec3 result)
        {
            int num = Mathf.Max(Mathf.RoundToInt(Mathf.Sqrt(cellRect.Area)), 5);
            for (int i = 0; i < num; i++)
            {
                IntVec3 randomCell = cellRect.RandomCell;
                if (predicate(randomCell))
                {
                    result = randomCell;
                    __result = true;
                    return false;
                }
            }
            if(tmpCells == null)
            {
                tmpCells = new List<IntVec3>();
            } else
            {
                tmpCells.Clear();
            }
            foreach (IntVec3 item in cellRect)
            {
                tmpCells.Add(item);
            }

            tmpCells.Shuffle();
            int j = 0;
            for (int count = tmpCells.Count; j < count; j++)
            {
                if (predicate(tmpCells[j]))
                {
                    result = tmpCells[j];
                    __result = true;
                    return false;
                }
            }

            result = IntVec3.Invalid;
            __result = false;
            return false;
        }

        public static bool FindNoWipeSpawnLocNear(ref IntVec3 __result, IntVec3 near, Map map, ThingDef thingToSpawn, Rot4 rot, int maxDist = 2, Predicate<IntVec3> extraValidator = null)
        {
            int num = GenRadial.NumCellsInRadius(maxDist);
            IntVec3 result = IntVec3.Invalid;
            float num2 = 0f;
            for (int i = 0; i < num; i++)
            {
                IntVec3 intVec = near + GenRadial.RadialPattern[i];
                if (!intVec.InBounds(map))
                {
                    continue;
                }

                CellRect cellRect = GenAdj.OccupiedRect(intVec, rot, thingToSpawn.size);
                if (!cellRect.InBounds(map) || !GenSight.LineOfSight(near, intVec, map, skipFirstCell: true) || (extraValidator != null && !extraValidator(intVec)) || (thingToSpawn.category == ThingCategory.Building && !GenConstruct.CanBuildOnTerrain(thingToSpawn, intVec, map, rot)))
                {
                    continue;
                }

                bool flag = false;
                bool flag2 = false;
                if (tmpUniqueWipedThings == null)
                {
                    tmpUniqueWipedThings = new List<Thing>();
                }
                else
                {
                    tmpUniqueWipedThings.Clear();
                }
                foreach (IntVec3 item in cellRect)
                {
                    if (item.Impassable(map))
                    {
                        flag2 = true;
                    }

                    List<Thing> thingList = item.GetThingList(map);
                    for (int j = 0; j < thingList.Count; j++)
                    {
                        if (thingList[j] is Pawn)
                        {
                            flag = true;
                        }
                        else if (GenSpawn.SpawningWipes(thingToSpawn, thingList[j].def) && !tmpUniqueWipedThings.Contains(thingList[j]))
                        {
                            tmpUniqueWipedThings.Add(thingList[j]);
                        }
                    }
                }

                if (flag && thingToSpawn.passability == Traversability.Impassable)
                {
                    tmpUniqueWipedThings.Clear();
                    continue;
                }

                if (flag2 && thingToSpawn.category == ThingCategory.Item)
                {
                    tmpUniqueWipedThings.Clear();
                    continue;
                }

                float num3 = 0f;
                for (int k = 0; k < tmpUniqueWipedThings.Count; k++)
                {
                    if (tmpUniqueWipedThings[k].def.category == ThingCategory.Building && !tmpUniqueWipedThings[k].def.costList.NullOrEmpty() && tmpUniqueWipedThings[k].def.costStuffCount == 0)
                    {
                        List<ThingDefCountClass> list = tmpUniqueWipedThings[k].CostListAdjusted();
                        for (int l = 0; l < list.Count; l++)
                        {
                            num3 += list[l].thingDef.GetStatValueAbstract(StatDefOf.MarketValue) * list[l].count * tmpUniqueWipedThings[k].stackCount;
                        }
                    }
                    else
                    {
                        num3 += tmpUniqueWipedThings[k].MarketValue * tmpUniqueWipedThings[k].stackCount;
                    }

                    if (tmpUniqueWipedThings[k].def.category == ThingCategory.Building || tmpUniqueWipedThings[k].def.category == ThingCategory.Item)
                    {
                        num3 = Mathf.Max(num3, 0.001f);
                    }
                }

                tmpUniqueWipedThings.Clear();
                if (!result.IsValid || num3 < num2)
                {
                    if (num3 == 0f)
                    {
                        __result = intVec;
                        return false;
                    }

                    result = intVec;
                    num2 = num3;
                }
            }

            if (!result.IsValid)
            {
                __result = near;
                return false;
            }

            __result = result;
            return false;
        }

        public static bool RandomRegionNear(ref Region __result, Region root, int maxRegions, TraverseParms traverseParms, Predicate<Region> validator = null, Pawn pawnToAllow = null, RegionType traversableRegionTypes = RegionType.Set_Passable)
        {
            if (root == null)
            {
                throw new ArgumentNullException("root");
            }

            if (maxRegions <= 1)
            {
                __result = root;
                return false;
            }
            if (workingRegions == null)
            {
                workingRegions = new List<Region>();
            }
            else
            {
                workingRegions.Clear();
            }
            RegionTraverser.BreadthFirstTraverse(root, (Region from, Region r) => (validator == null || validator(r)) && r.Allows(traverseParms, isDestination: true) && (pawnToAllow == null || !r.IsForbiddenEntirely(pawnToAllow)), delegate (Region r)
            {
                workingRegions.Add(r);
                return false;
            }, maxRegions, traversableRegionTypes);
            Region result = workingRegions.RandomElementByWeight((Region r) => r.CellCount);
            //workingRegions.Clear();
            __result = result;
            return false;
        }
        public static bool TryFindRandomEdgeCellWith(ref bool __result, Predicate<IntVec3> validator, Map map, float roadChance, out IntVec3 result)
        {
            if (Rand.Chance(roadChance))
            {
                bool flag = map.roadInfo.roadEdgeTiles.Where((IntVec3 c) => validator(c)).TryRandomElement(out result);
                if (flag)
                {
                    __result = flag;
                    return false;
                }
            }

            for (int i = 0; i < 100; i++)
            {
                result = CellFinder.RandomEdgeCell(map);
                if (validator(result))
                {
                    __result = true;
                    return false;
                }
            }

            if (mapEdgeCells == null || map.Size != mapEdgeCellsSize)
            {
                mapEdgeCellsSize = map.Size;
                mapEdgeCells = new List<IntVec3>();
                foreach (IntVec3 edgeCell in CellRect.WholeMap(map).EdgeCells)
                {
                    mapEdgeCells.Add(edgeCell);
                }
            }

            mapEdgeCells.Shuffle();
            for (int j = 0; j < mapEdgeCells.Count; j++)
            {
                try
                {
                    if (validator(mapEdgeCells[j]))
                    {
                        result = mapEdgeCells[j];
                        __result = true;
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(string.Concat("TryFindRandomEdgeCellWith exception validating ", mapEdgeCells[j], ": ", ex.ToString()));
                }
            }

            result = IntVec3.Invalid;
            __result = false;
            return false;
        }
        public static bool TryFindRandomEdgeCellWith(ref bool __result, Predicate<IntVec3> validator, Map map, Rot4 dir, float roadChance, out IntVec3 result)
        {
            if (Rand.Value < roadChance)
            {
                bool flag = map.roadInfo.roadEdgeTiles.Where((IntVec3 c) => validator(c) && c.OnEdge(map, dir)).TryRandomElement(out result);
                if (flag)
                {
                    return flag;
                }
            }

            for (int i = 0; i < 100; i++)
            {
                result = CellFinder.RandomEdgeCell(dir, map);
                if (validator(result))
                {
                    return true;
                }
            }

            int asInt = dir.AsInt;
            if(mapSingleEdgeCells == null)
            {
                mapSingleEdgeCells = new List<IntVec3>[4];
            }
            if (mapSingleEdgeCells[asInt] == null || map.Size != mapSingleEdgeCellsSize)
            {
                mapSingleEdgeCellsSize = map.Size;
                mapSingleEdgeCells[asInt] = new List<IntVec3>();
                foreach (IntVec3 edgeCell in CellRect.WholeMap(map).GetEdgeCells(dir))
                {
                    mapSingleEdgeCells[asInt].Add(edgeCell);
                }
            }

            List<IntVec3> list = mapSingleEdgeCells[asInt];
            list.Shuffle();
            int j = 0;
            for (int count = list.Count; j < count; j++)
            {
                try
                {
                    if (validator(list[j]))
                    {
                        result = list[j];
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(string.Concat("TryFindRandomEdgeCellWith exception validating ", list[j], ": ", ex.ToString()));
                }
            }

            result = IntVec3.Invalid;
            return false;
        }


    }
}

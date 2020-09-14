using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace RimThreaded
{

    public class CellFinder_Patch
    {
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
			List<IntVec3> workingCells = new List<IntVec3>();
			//workingCells.Clear();
			workingCells.AddRange(reg.Cells);
			workingCells.Shuffle<IntVec3>();
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

        private static IEnumerable<IntVec3> GetAdjacentCardinalCellsForBestStandCell(
          IntVec3 x,
          float radius,
          Pawn pawn)
        {
            if ((double)(x - pawn.Position).LengthManhattan <= (double)radius)
            {
                for (int i = 0; i < 4; ++i)
                {
                    IntVec3 c = x + GenAdj.CardinalDirections[i];
                    if (c.InBounds(pawn.Map) && c.Walkable(pawn.Map) && (!(c.GetEdifice(pawn.Map) is Building_Door edifice) || edifice.CanPhysicallyPass(pawn)))
                        yield return c;
                }
            }
        }
        public static bool TryFindBestPawnStandCell(ref bool __result, Pawn forPawn, out IntVec3 cell, bool cellByCell = false)
		{
            cell = IntVec3.Invalid;
            int num1 = -1;
            float radius = 10f;
            Dictionary<IntVec3, float> tmpDistances = new Dictionary<IntVec3, float>();
            Dictionary<IntVec3, IntVec3> tmpParents = new Dictionary<IntVec3, IntVec3>();
            DijkstraIntVec3 dijkstraIntVec3 = new DijkstraIntVec3();
            while (true)
            {
                tmpDistances.Clear();
                tmpParents.Clear();
                dijkstraIntVec3.Run(forPawn.Position, (Func<IntVec3, IEnumerable<IntVec3>>)(x => GetAdjacentCardinalCellsForBestStandCell(x, radius, forPawn)), (Func<IntVec3, IntVec3, float>)((from, to) =>
                {
                    float num2 = 1f;
                    if (from.x != to.x && from.z != to.z)
                        num2 = 1.414214f;
                    if (!to.Standable(forPawn.Map))
                        num2 += 3f;
                    if (PawnUtility.AnyPawnBlockingPathAt(to, forPawn, false, false, false))
                    {
                        if (to.GetThingList(forPawn.Map).Find((Predicate<Thing>)(x => x is Pawn && x.HostileTo((Thing)forPawn))) != null)
                            num2 += 40f;
                        else
                            num2 += 15f;
                    }
                    if (to.GetEdifice(forPawn.Map) is Building_Door edifice && !edifice.FreePassage)
                    {
                        if (edifice.PawnCanOpen(forPawn))
                            num2 += 6f;
                        else
                            num2 += 50f;
                    }
                    return num2;
                }), tmpDistances, tmpParents);
                if (tmpDistances.Count != num1)
                {
                    float num2 = 0.0f;
                    foreach (KeyValuePair<IntVec3, float> tmpDistance in tmpDistances)
                    {
                        if ((!cell.IsValid || (double)tmpDistance.Value < (double)num2) && (tmpDistance.Key.Walkable(forPawn.Map) && !PawnUtility.AnyPawnBlockingPathAt(tmpDistance.Key, forPawn, false, false, false)))
                        {
                            Building_Door door = tmpDistance.Key.GetDoor(forPawn.Map);
                            if (door == null || door.FreePassage)
                            {
                                cell = tmpDistance.Key;
                                num2 = tmpDistance.Value;
                            }
                        }
                    }
                    if (!cell.IsValid)
                    {
                        if ((double)radius <= (double)forPawn.Map.Size.x || (double)radius <= (double)forPawn.Map.Size.z)
                        {
                            radius *= 2f;
                            num1 = tmpDistances.Count;
                        }
                        else
                            goto label_23;
                    }
                    else
                        goto label_11;
                }
                else
                    break;
            }
            __result = false;
            return false;
        label_11:
            if (!cellByCell)
            {
                __result = true;
                return false;
            }
            IntVec3 c = cell;
            int num3 = 0;
            for (; c.IsValid && c != forPawn.Position; c = tmpParents[c])
            {
                ++num3;
                if (num3 >= 10000)
                {
                    Log.Error("Too many iterations.", false);
                    break;
                }
                if (c.Walkable(forPawn.Map))
                {
                    Building_Door door = c.GetDoor(forPawn.Map);
                    if (door == null || door.FreePassage)
                        cell = c;
                }
            }
            __result = true;
            return false;
        label_23:
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
                Log.ErrorOnce("Tried to find reachable cell in a null map", 61037855, false);
                result = IntVec3.Invalid;
                __result = false;
                return false;
            }
            Region region = root.GetRegion(map, RegionType.Set_Passable);
            if (region == null)
            {
                result = IntVec3.Invalid;
                __result = false;
                return false;
            }
            List<Region> workingRegions = new List<Region>();
            float radSquared = radius * radius;
            RegionTraverser.BreadthFirstTraverse(region, (RegionEntryPredicate)((from, r) =>
            {
                if (!r.Allows(traverseParms, true) || (double)radius <= 1000.0 && (double)r.extentsClose.ClosestDistSquaredTo(root) > (double)radSquared)
                    return false;
                return regionValidator == null || regionValidator(r);
            }), (RegionProcessor)(r =>
            {
                workingRegions.Add(r);
                return false;
            }), maxRegions, RegionType.Set_Passable);
            while (workingRegions.Count > 0)
            {
                Region reg = workingRegions.RandomElementByWeight<Region>((Func<Region, float>)(r => (float)r.CellCount));
                if (reg.TryFindRandomCellInRegion((Predicate<IntVec3>)(c =>
                {
                    if ((double)(c - root).LengthHorizontalSquared > (double)radSquared)
                        return false;
                    return cellValidator == null || cellValidator(c);
                }), out result))
                {
                    workingRegions.Clear();
                    __result = true;
                    return false;
                }
                workingRegions.Remove(reg);
            }
            result = IntVec3.Invalid;
            __result = false;
            return false;
        }

    }
}

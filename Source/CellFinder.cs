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

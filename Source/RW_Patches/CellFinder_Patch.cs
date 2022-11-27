using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace RimThreaded.RW_Patches
{
    class CellFinder_Patch
    {
        [ThreadStatic] public static List<IntVec3>[] mapSingleEdgeCells;

        internal static void InitializeThreadStatics()
        {
            mapSingleEdgeCells = new List<IntVec3>[4];
        }

        internal static void RunNonDestructivePatches()
        {
            Type original = typeof(CellFinder);
            Type patched = typeof(CellFinder_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(TryFindRandomCellNear), null, false);
        }
        internal static void RunDestructivePatches()
        {
            Type original = typeof(CellFinder);
            Type patched = typeof(CellFinder_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(RandomRegionNear));
        }
        public static bool TryFindRandomCellNear(ref bool __result, IntVec3 root,
              Map map,
              int squareRadius,
              Predicate<IntVec3> validator,
              ref IntVec3 result,
              int maxTries = -1
            )
        {
            if (map == null)
            {
                __result = false;
                return false;
            }
            return true;
        }
        public static bool RandomRegionNear(ref Region __result, Region root, int maxRegions, TraverseParms traverseParms, Predicate<Region> validator = null, Pawn pawnToAllow = null, RegionType traversableRegionTypes = RegionType.Set_Passable)
        {
            if (root == null)
            {
                //start change
                //throw new ArgumentNullException("root");
                Log.Warning("TryFindRandomRegionNear received a null root Region");
                __result = null;
                return false;
            }
            if (maxRegions <= 1)
            {
                __result = root;
                return false;
            }
            CellFinder.workingRegions.Clear();
            RegionTraverser.BreadthFirstTraverse(root, (Region from, Region r) => (validator == null || validator(r)) && r.Allows(traverseParms, isDestination: true) && (pawnToAllow == null || !r.IsForbiddenEntirely(pawnToAllow)), delegate (Region r)
            {
                CellFinder.workingRegions.Add(r);
                return false;
            }, maxRegions, traversableRegionTypes);
            Region result = CellFinder.workingRegions.RandomElementByWeight((Region r) => r.CellCount);
            CellFinder.workingRegions.Clear();
            __result = result;
            return false;
        }
    }
}

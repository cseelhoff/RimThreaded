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
    }
}

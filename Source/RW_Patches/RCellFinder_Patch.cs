using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace RimThreaded.RW_Patches
{
    class RCellFinder_Patch
    {

        internal static void RunDestructivePatches()
        {
            Type original = typeof(RCellFinder);
            Type patched = typeof(RCellFinder_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(TryFindRandomCellInRegionUnforbidden));
        }
        
        public static bool TryFindRandomCellInRegionUnforbidden(ref bool __result, Region reg, Pawn pawn, Predicate<IntVec3> validator, out IntVec3 result)
        {
            if (reg == null)
            {
                //start changes
                //throw new ArgumentNullException("reg");
                Log.Warning("TryFindRandomCellInRegionUnforbidden received null reg Region");
                result = IntVec3.Invalid;
                __result = false;
                return false;
                //end changes
            }
            if (reg.IsForbiddenEntirely(pawn))
            {
                result = IntVec3.Invalid;
                __result = false;
                return false;
            }
            __result = reg.TryFindRandomCellInRegion((IntVec3 c) => !c.IsForbidden(pawn) && (validator == null || validator(c)), out result);
            return false;
        }
    }
}

using System;
using Verse;

namespace RimThreaded.RW_Patches
{
    class GenGrid_Patch
    {
        internal static void RunNonDestructivePatches()
        {
            Type original = typeof(GenGrid);
            Type patched = typeof(GenGrid_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(InBounds), new Type[] { typeof(IntVec3), typeof(Map) }, false);
        }
        public static bool InBounds(ref bool __result, IntVec3 c, Map map)
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

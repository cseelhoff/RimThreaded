using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimThreaded
{
    class GenGrid_Patch
    {
        internal static void RunNonDestructivePatches()
        {
            Type original = typeof(GenGrid);
            Type patched = typeof(GenGrid_Patch);
            //RimThreadedHarmony.Prefix(original, patched, nameof(InBounds), new Type[] { typeof(IntVec3), typeof(Map) }, false);
        }
        public static bool InBounds(ref bool __result, IntVec3 c, Map map)
        {
            if(map == null)
            {
                __result = false;
                return false;
            }
            return true;
        }
    }
}

using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimThreaded
{
    class RestUtility_Patch
    {
        internal static void RunNonDestructivePatches()
        {
            Type original = typeof(RestUtility);
            Type patched = typeof(RestUtility_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(CurrentBed),null, false);
        }
        public static bool CurrentBed(Pawn __instance, ref Building_Bed __result)
        {
            if (__instance == null)
            {
                __result = null;
                return false;
            }
            return true;
        }
    }
}

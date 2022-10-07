using Verse;
using System;

namespace RimThreaded.RW_Patches
{
    public class Verb_Patch
    {

        static readonly Type original = typeof(Verb);
        static readonly Type patched = typeof(Verb_Patch);

        internal static void RunDestructivePatches()
        {
            RimThreadedHarmony.Prefix(original, patched, "get_DirectOwner");
        }

        public static bool get_DirectOwner(Verb __instance, ref IVerbOwner __result)
        {
            if (__instance.verbTracker == null)
            {
                __result = null;
            }
            else
            {
                return true;
            }
            return false;
        }

    }
}

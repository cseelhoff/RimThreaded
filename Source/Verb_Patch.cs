using System.Collections.Generic;
using Verse;
using System;

namespace RimThreaded
{
    public class Verb_Patch
    {
        [ThreadStatic] public static List<IntVec3> tempLeanShootSources = new List<IntVec3>();
        [ThreadStatic] public static List<IntVec3> tempDestList = new List<IntVec3>();

        static readonly Type original = typeof(Verb);
        static readonly Type patched = typeof(Verb_Patch);

        public static void InitializeThreadStatics()
        {
            tempLeanShootSources = new List<IntVec3>();
            tempDestList = new List<IntVec3>();
        }
        internal static void RunNonDestructivePatches()
        {
            RimThreadedHarmony.AddAllMatchingFields(original, patched);
            RimThreadedHarmony.TranspileFieldReplacements(original, "TryFindShootLineFromTo");
            RimThreadedHarmony.TranspileFieldReplacements(original, "CanHitFromCellIgnoringRange");
        }
        internal static void RunDestructivePatches()
        {
            RimThreadedHarmony.Prefix(original, patched, "get_DirectOwner");
        }

        public static bool get_DirectOwner(Verb __instance, ref IVerbOwner __result)
        {            
            if (__instance.verbTracker == null)
            {
                __result = null; 
            } else
            {
                return true;
            }
            return false;
        }

    }
}

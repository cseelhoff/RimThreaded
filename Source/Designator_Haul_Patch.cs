using System;
using RimWorld;
using Verse;

namespace RimThreaded
{
    class Designator_Haul_Patch
    {
        private static readonly Type Original = typeof(Designator_Haul);
        private static readonly Type Patched = typeof(Designator_Haul_Patch);
        public static void RunNonDestructivePatches()
        {
            RimThreadedHarmony.Postfix(Original, Patched, "DesignateThing");
        }

        public static void DesignateThing(Designator_Haul __instance, Thing t)
        {
            HaulingCache.ReregisterHaulableItem(t);
        }

    }
}

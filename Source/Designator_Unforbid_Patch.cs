using System;
using RimWorld;
using Verse;

namespace RimThreaded
{
    class Designator_Unforbid_Patch
    {
        private static readonly Type Original = typeof(Designator_Unforbid);
        private static readonly Type Patched = typeof(Designator_Unforbid_Patch);
        public static void RunNonDestructivePatches()
        {
            RimThreadedHarmony.Postfix(Original, Patched, "DesignateThing");
        }

        public static void DesignateThing(Designator_Unforbid __instance, Thing t)
        {
            HaulingCache.ReregisterHaulableItem(t);
        }

    }
}

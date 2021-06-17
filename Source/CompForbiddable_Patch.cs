using RimWorld;
using System;

namespace RimThreaded
{
    class CompForbiddable_Patch
    {
        private static readonly Type Original = typeof(CompForbiddable);
        private static readonly Type Patched = typeof(CompForbiddable_Patch);
        public static void RunNonDestructivePatches()
        {
            RimThreadedHarmony.Postfix(Original, Patched, "set_Forbidden");
        }

        public static void set_Forbidden(CompForbiddable __instance, bool value)
        {
            if (__instance.parent.Map != null)
            {
                HaulingCache.ReregisterHaulableItem(__instance.parent);
            }
        }
    }
}

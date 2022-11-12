using RimWorld;
using System;

namespace RimThreaded.RW_Patches
{
    class Filth_Patch
    {
        public static readonly Type original = typeof(Filth);
        public static readonly Type patched = typeof(Filth_Patch);

        public static void RunNonDestructivePatches()
        {
            RimThreadedHarmony.Postfix(original, patched, nameof(ThinFilth));
        }

        public static void ThinFilth(Filth __instance)
        {
            if (__instance.Spawned)
            {
                if (__instance.thickness < 0)
                {
                    __instance.Destroy();
                }
            }
        }


    }
}
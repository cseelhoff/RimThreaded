using RimWorld;
using System;
using Verse;

namespace RimThreaded.RW_Patches
{
    class JobDriver_TendPatient_Patch
    {
        internal static void RunDestructivePatches()
        {
            Type original = typeof(JobDriver_TendPatient);
            Type patched = typeof(JobDriver_TendPatient_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(get_Deliveree));
        }
        public static bool get_Deliveree(JobDriver_TendPatient __instance, ref Pawn __result)
        {
            __result = (Pawn)__instance?.job?.targetA.Thing;
            return false;
        }
    }
}

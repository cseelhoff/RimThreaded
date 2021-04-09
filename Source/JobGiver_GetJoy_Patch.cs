using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimThreaded
{
    class JobGiver_GetJoy_Patch
    {
        internal static void RunDestructivePatches()
        {
            Type original = typeof(JobGiver_GetJoy);
            Type patched = typeof(JobGiver_GetJoy_Patch);
            RimThreadedHarmony.Prefix(original, patched, "TryGiveJobFromJoyGiverDefDirect");
        }

        [ThreadStatic] public static JoyGiverDef defx;
        [ThreadStatic] public static Pawn pawnx;

        public static bool TryGiveJobFromJoyGiverDefDirect(JobGiver_Work __instance, ref Job __result, JoyGiverDef def, Pawn pawn)
        {
            pawnx = pawn;
            defx = def;
            __result = defx.Worker.TryGiveJob(pawnx);
            return false;
        }

    }
}
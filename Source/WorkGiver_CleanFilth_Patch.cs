using System;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimThreaded
{
    class WorkGiver_CleanFilth_Patch// : WorkGiver_Scanner
    {
        public static void RunDestructivePatches()
        {
            //Type original = typeof(WorkGiver_CleanFilth); // Deligate?
            Type patched = typeof(WorkGiver_CleanFilth_Patch);
            //RimThreadedHarmony.Prefix(original, patched, "HasJobOnThing");
        }

        private static int MinTicksSinceThickened = 600;

        public static bool HasJobOnThing(ref bool __result, Pawn pawn, Thing t, bool forced = false)
        {
            Filth filth = t as Filth;
            if (filth?.Map?.areaManager?.Home != null && pawn != null)
            {
                __result = filth.Map.areaManager.Home[filth.Position] && pawn.CanReserve(t, 1, -1, null, forced) && filth.TicksSinceThickened >= MinTicksSinceThickened;
            }
            return false;
        }
        public static bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return t is Filth filth && filth.Map.areaManager.Home[filth.Position] && pawn.CanReserve(t, 1, -1, null, forced) && filth.TicksSinceThickened >= MinTicksSinceThickened;
        }
    }
}
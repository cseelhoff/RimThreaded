using RimWorld;
using System;
using Verse;

namespace RimThreaded.RW_Patches
{
    class Zone_Patch
    {
        internal static void RunNonDestructivePatches()
        {
            Type original = typeof(Zone);
            Type patched = typeof(Zone_Patch);
            RimThreadedHarmony.Postfix(original, patched, "CheckAddHaulDestination");
        }

        public static void CheckAddHaulDestination(Zone __instance)
        {
            if (Current.ProgramState == ProgramState.Playing)
            {
                if (__instance is Zone_Growing zone)
                {
                    //Log.Message("Adding growing zone cell to awaiting plant cells");
                    foreach (IntVec3 c in zone.cells)
                    {
                        JumboCell.ReregisterObject(zone.Map, c, RimThreaded.plantSowing_Cache);
                    }
                }
            }
        }
    }
}

using RimWorld;
using System;
using Verse;

namespace RimThreaded.RW_Patches
{
    class Zone_Growing_Patch
    {
        internal static void RunNonDestructivePatches()
        {
            Type original = typeof(Zone_Growing);
            Type patched = typeof(Zone_Growing_Patch);
            RimThreadedHarmony.Postfix(original, patched, nameof(SetPlantDefToGrow));
        }

        public static void SetPlantDefToGrow(Zone_Growing __instance, ThingDef plantDef)
        {
            if (Current.ProgramState == ProgramState.Playing)
            {
                foreach (IntVec3 c in __instance.cells)
                {
                    JumboCell.ReregisterObject(__instance.Map, c, RimThreaded.plantSowing_Cache);
                }
            }
        }
    }
}

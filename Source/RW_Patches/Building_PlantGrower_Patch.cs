using RimWorld;
using System;
using Verse;

namespace RimThreaded.RW_Patches
{
    class Building_PlantGrower_Patch
    {
        internal static void RunNonDestructivePatches()
        {
            Type original = typeof(Building_PlantGrower);
            Type patched = typeof(Building_PlantGrower_Patch);
            RimThreadedHarmony.Postfix(original, patched, nameof(SetPlantDefToGrow));
        }

        public static void SetPlantDefToGrow(Building_PlantGrower __instance, ThingDef plantDef)
        {
            if (Current.ProgramState == ProgramState.Playing)
            {
                foreach (IntVec3 c in __instance.OccupiedRect())
                {
                    JumboCell.ReregisterObject(__instance.Map, c, RimThreaded.plantSowing_Cache);
                }
            }
        }
    }
}

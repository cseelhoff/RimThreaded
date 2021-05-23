using System;
using RimWorld;
using Verse;

namespace RimThreaded
{

    public class StoryState_Patch
    {
        internal static void RunDestructivePatches()
        {
            Type original = typeof(StoryState);
            Type patched = typeof(StoryState_Patch);
            RimThreadedHarmony.Prefix(original, patched, "RecordPopulationIncrease");
        }
        public static bool RecordPopulationIncrease(StoryState __instance)
        {
            int count = PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_FreeColonists.Count;
            lock (__instance.colonistCountTicks)
            {
                if (!__instance.colonistCountTicks.ContainsKey(count))
                {
                    __instance.colonistCountTicks.Add(count, Find.TickManager.TicksGame);
                }
            }
            return false;
        }

    }
}

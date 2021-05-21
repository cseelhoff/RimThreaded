using HarmonyLib;
using System;
using System.Collections.Generic;
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
        public static AccessTools.FieldRef<StoryState, Dictionary<int, int>> colonistCountTicks =
			AccessTools.FieldRefAccess<StoryState, Dictionary<int, int>>("colonistCountTicks");
        public static bool RecordPopulationIncrease(StoryState __instance)
        {
            int count = PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_FreeColonists.Count;
            lock (colonistCountTicks(__instance))
            {
                if (!colonistCountTicks(__instance).ContainsKey(count))
                {
                    colonistCountTicks(__instance).Add(count, Find.TickManager.TicksGame);
                }
            }
            return false;
        }

    }
}

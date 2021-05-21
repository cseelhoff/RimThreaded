using HarmonyLib;
using System.Collections.Generic;
using Verse;
using System;

namespace RimThreaded
{

    public class MapTemperature_Patch
    {
        [ThreadStatic] public static HashSet<RoomGroup> fastProcessedRoomGroups;

        public static void InitializeThreadStatics()
        {
            fastProcessedRoomGroups = new HashSet<RoomGroup>();
        }

        public static void RunNonDestructivePatches()
        {
            Type original = typeof(MapTemperature);
            Type patched = typeof(MapTemperature_Patch);
            RimThreadedHarmony.AddAllMatchingFields(original, patched);
            RimThreadedHarmony.TranspileFieldReplacements(original, "MapTemperatureTick");
        }

    }
}

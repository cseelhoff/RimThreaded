using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;

namespace RimThreaded.RW_Patches
{

    public class WealthWatcher_Patch
    {

        static Type original = typeof(WealthWatcher);
        static Type patched = typeof(WealthWatcher_Patch);

        public static void RunDestructivePatches()
        {
            RimThreadedHarmony.Prefix(original, patched, "ResetStaticData");
        }

        public static bool ResetStaticData(WealthWatcher __instance)
        {
            int num = -1;
            List<TerrainDef> allDefsListForReading = DefDatabase<TerrainDef>.AllDefsListForReading;
            for (int i = 0; i < allDefsListForReading.Count; i++)
            {
                num = Mathf.Max(num, allDefsListForReading[i].index);
            }

            float[] newCachedTerrainMarketValue = new float[num + 1];
            for (int j = 0; j < allDefsListForReading.Count; j++)
            {
                newCachedTerrainMarketValue[allDefsListForReading[j].index] = allDefsListForReading[j].GetStatValueAbstract(StatDefOf.MarketValue);
            }
            WealthWatcher.cachedTerrainMarketValue = newCachedTerrainMarketValue;
            return false;
        }


    }
}

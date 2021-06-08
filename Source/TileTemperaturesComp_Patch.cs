using System;
using RimWorld;
using Verse;
using RimWorld.Planet;
using System.Threading;
using static RimWorld.Planet.TileTemperaturesComp;

namespace RimThreaded
{

    public class TileTemperaturesComp_Patch
    {
		private static int startIndex;
		private static int endIndex;
		private static int[] usedSlots;
        private static CachedTileTemperatureData[] cache;
        private static int tileCount;

        internal static void RunDestructivePatches()
        {
            Type original = typeof(TileTemperaturesComp);
            Type patched = typeof(TileTemperaturesComp_Patch);
            RimThreadedHarmony.Prefix(original, patched, "WorldComponentTick");
            RimThreadedHarmony.Prefix(original, patched, "ClearCaches");
            RimThreadedHarmony.Prefix(original, patched, "GetOutdoorTemp");
            RimThreadedHarmony.Prefix(original, patched, "GetSeasonalTemp");
            RimThreadedHarmony.Prefix(original, patched, "OutdoorTemperatureAt");
            RimThreadedHarmony.Prefix(original, patched, "OffsetFromDailyRandomVariation");
            RimThreadedHarmony.Prefix(original, patched, "AverageTemperatureForTwelfth");
            RimThreadedHarmony.Prefix(original, patched, "SeasonAcceptableFor");
            RimThreadedHarmony.Prefix(original, patched, "OutdoorTemperatureAcceptableFor");
            RimThreadedHarmony.Prefix(original, patched, "SeasonAndOutdoorTemperatureAcceptableFor");
        }

        public static bool WorldComponentTick(TileTemperaturesComp __instance)
        {
			for (int i = startIndex; i < endIndex; i++)
			{
                if (cache[usedSlots[i % tileCount]] == null)
                {
                    Interlocked.Increment(ref startIndex);
                }
                else
                {
                    cache[usedSlots[i % tileCount]].CheckCache();
                }
			}

			if (Find.TickManager.TicksGame % 300 == 84 && startIndex < endIndex)
			{
				cache[usedSlots[(Interlocked.Increment(ref startIndex) - 1) % tileCount]] = null;				
			}
			return false;
        }
        
        public static bool ClearCaches(TileTemperaturesComp __instance)
        {
            tileCount = Find.WorldGrid.TilesCount;
			cache = new CachedTileTemperatureData[tileCount];
			usedSlots = new int[tileCount];
            return false;
		}
		private static CachedTileTemperatureData RetrieveCachedData2(TileTemperaturesComp __instance, int tile)
        {
            if (cache[tile] != null)
			{
				return cache[tile];
			}

			cache[tile] = new CachedTileTemperatureData(tile);
			usedSlots[(Interlocked.Increment(ref endIndex) - 1) % tileCount] = tile;
			return cache[tile];
		}

        public static bool GetOutdoorTemp(TileTemperaturesComp __instance, ref float __result, int tile)
        {
            __result = RetrieveCachedData2(__instance, tile).GetOutdoorTemp();
            return false;
        }

        public static bool GetSeasonalTemp(TileTemperaturesComp __instance, ref float __result, int tile)
        {
            __result = RetrieveCachedData2(__instance, tile).GetSeasonalTemp();
            return false;
        }

        public static bool OutdoorTemperatureAt(TileTemperaturesComp __instance, ref float __result, int tile, int absTick)
        {
            __result = RetrieveCachedData2(__instance, tile).OutdoorTemperatureAt(absTick);
            return false;
        }

        public static bool OffsetFromDailyRandomVariation(TileTemperaturesComp __instance, ref float __result, int tile, int absTick)
        {
            __result = RetrieveCachedData2(__instance, tile).OffsetFromDailyRandomVariation(absTick);
            return false;
        }

        public static bool AverageTemperatureForTwelfth(TileTemperaturesComp __instance, ref float __result, int tile, Twelfth twelfth)
        {
            __result = RetrieveCachedData2(__instance, tile).AverageTemperatureForTwelfth(twelfth);
            return false;
        }

        public static bool SeasonAcceptableFor(TileTemperaturesComp __instance, ref bool __result, int tile, ThingDef animalRace)
        {
            float seasonalTemp = __instance.GetSeasonalTemp(tile);
            if (seasonalTemp > animalRace.GetStatValueAbstract(StatDefOf.ComfyTemperatureMin))
            {
                __result = seasonalTemp < animalRace.GetStatValueAbstract(StatDefOf.ComfyTemperatureMax);
                return false;
            }
            __result = false;
            return false;
        }

        public static bool OutdoorTemperatureAcceptableFor(TileTemperaturesComp __instance, ref bool __result, int tile, ThingDef animalRace)
        {
            float outdoorTemp = __instance.GetOutdoorTemp(tile);
            if (outdoorTemp > animalRace.GetStatValueAbstract(StatDefOf.ComfyTemperatureMin))
            {
                __result = outdoorTemp < animalRace.GetStatValueAbstract(StatDefOf.ComfyTemperatureMax);
                return false;
            }
            __result = false;
            return false;
        }

        public static bool SeasonAndOutdoorTemperatureAcceptableFor(TileTemperaturesComp __instance, ref bool __result, int tile, ThingDef animalRace)
        {
            if (__instance.SeasonAcceptableFor(tile, animalRace))
            {
                __result = __instance.OutdoorTemperatureAcceptableFor(tile, animalRace);
                return false;
            }
            __result = false;
            return false;
        }

    }


}

using System;
using RimWorld;
using Verse;
using RimWorld.Planet;
using Verse.Noise;
using System.Threading;

namespace RimThreaded
{

    public class TileTemperaturesComp_Patch
    {
		private static int startIndex = 0;
		private static int endIndex = 0;
		private static int[] usedSlots;
		private static CachedTileTemperatureData2[] cache;
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
        private class CachedTileTemperatureData2
        {
            private int tile;

            private int tickCachesNeedReset = int.MinValue;

            private float cachedOutdoorTemp = float.MinValue;

            private float cachedSeasonalTemp = float.MinValue;

            private float[] twelfthlyTempAverages;

            private Perlin dailyVariationPerlinCached;

            private const int CachedTempUpdateInterval = 60;

            public CachedTileTemperatureData2(int tile)
            {
                this.tile = tile;
                int seed = Gen.HashCombineInt(tile, 199372327);
                dailyVariationPerlinCached = new Perlin(4.9999998736893758E-06, 2.0, 0.5, 3, seed, QualityMode.Medium);
                twelfthlyTempAverages = new float[12];
                for (int i = 0; i < 12; i++)
                {
                    twelfthlyTempAverages[i] = GenTemperature.AverageTemperatureAtTileForTwelfth(tile, (Twelfth)i);
                }

                CheckCache();
            }

            public float GetOutdoorTemp()
            {
                return cachedOutdoorTemp;
            }

            public float GetSeasonalTemp()
            {
                return cachedSeasonalTemp;
            }

            public float OutdoorTemperatureAt(int absTick)
            {
                return CalculateOutdoorTemperatureAtTile(absTick, includeDailyVariations: true);
            }

            public float OffsetFromDailyRandomVariation(int absTick)
            {
                return (float)dailyVariationPerlinCached.GetValue(absTick, 0.0, 0.0) * 7f;
            }

            public float AverageTemperatureForTwelfth(Twelfth twelfth)
            {
                return twelfthlyTempAverages[(uint)twelfth];
            }

            public void CheckCache()
            {
                if (tickCachesNeedReset <= Find.TickManager.TicksGame)
                {
                    tickCachesNeedReset = Find.TickManager.TicksGame + 60;
                    Map map = Current.Game.FindMap(tile);
                    cachedOutdoorTemp = OutdoorTemperatureAt(Find.TickManager.TicksAbs);
                    if (map != null)
                    {
                        cachedOutdoorTemp += AggregateTemperatureOffset(map.gameConditionManager);
                    }

                    cachedSeasonalTemp = CalculateOutdoorTemperatureAtTile(Find.TickManager.TicksAbs, includeDailyVariations: false);
                }
            }

            private float AggregateTemperatureOffset(GameConditionManager gameConditionManager)
            {
                float num = 0f;
                for (int i = 0; i < gameConditionManager.ActiveConditions.Count; i++)
                {
                    num += gameConditionManager.ActiveConditions[i].TemperatureOffset();
                }

                if (gameConditionManager.Parent != null)
                {
                    num += AggregateTemperatureOffset(gameConditionManager.Parent);
                }

                return num;
            }

            private float CalculateOutdoorTemperatureAtTile(int absTick, bool includeDailyVariations)
            {
                if (absTick == 0)
                {
                    absTick = 1;
                }

                float num = Find.WorldGrid[tile].temperature + GenTemperature.OffsetFromSeasonCycle(absTick, tile);
                if (includeDailyVariations)
                {
                    num += OffsetFromDailyRandomVariation(absTick) + GenTemperature.OffsetFromSunCycle(absTick, tile);
                }

                return num;
            }
        }

        public static bool ClearCaches(TileTemperaturesComp __instance)
		{
			tileCount = Find.WorldGrid.TilesCount;
			cache = new CachedTileTemperatureData2[tileCount];
			usedSlots = new int[tileCount];
            return false;
		}
		private static CachedTileTemperatureData2 RetrieveCachedData2(TileTemperaturesComp __instance, int tile)
		{
			if (cache[tile] != null)
			{
				return cache[tile];
			}

			cache[tile] = new CachedTileTemperatureData2(tile);
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

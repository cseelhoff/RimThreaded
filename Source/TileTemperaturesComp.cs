using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using RimWorld.Planet;
using Verse.Noise;

namespace RimThreaded
{

    public class TileTemperaturesComp_Patch
    {
        public static AccessTools.FieldRef<TileTemperaturesComp, List<int>> usedSlots =
            AccessTools.FieldRefAccess<TileTemperaturesComp, List<int>>("usedSlots");
		public static Dictionary<TileTemperaturesComp, CachedTileTemperatureData2[]> cache2 = new Dictionary<TileTemperaturesComp, CachedTileTemperatureData2[]>();
        public static bool WorldComponentTick(TileTemperaturesComp __instance)
        {
			if(!cache2.TryGetValue(__instance, out CachedTileTemperatureData2[] cache3))
            {
				cache3 = new CachedTileTemperatureData2[Find.WorldGrid.TilesCount];
				cache2[__instance] = cache3;
			}

			List<int> usedSlots2 = usedSlots(__instance);

			for (int i = 0; i < usedSlots2.Count; i++)
            {
				int index1 = usedSlots2[i];
                CachedTileTemperatureData2 c3 = cache3[index1];
				if (null != c3)
				{
					c3.CheckCache();
				}
            }

            if (Find.TickManager.TicksGame % 300 == 84 && usedSlots(__instance).Any())
            {
				int index2 = usedSlots2[0];
				cache3[index2] = null;
				usedSlots2.RemoveAt(0);
            }
            return false;
        }

		public class CachedTileTemperatureData2
		{
			// Token: 0x0600B7AC RID: 47020 RVA: 0x00354924 File Offset: 0x00352B24
			public CachedTileTemperatureData2(int tile)
			{
				this.tile = tile;
				int seed = Gen.HashCombineInt(tile, 199372327);
				this.dailyVariationPerlinCached = new Perlin(4.9999998736893758E-06, 2.0, 0.5, 3, seed, QualityMode.Medium);
				this.twelfthlyTempAverages = new float[12];
				for (int i = 0; i < 12; i++)
				{
					this.twelfthlyTempAverages[i] = GenTemperature.AverageTemperatureAtTileForTwelfth(tile, (Twelfth)i);
				}
				this.CheckCache();
			}

			// Token: 0x0600B7AD RID: 47021 RVA: 0x003549C4 File Offset: 0x00352BC4
			public float GetOutdoorTemp()
			{
				return this.cachedOutdoorTemp;
			}

			// Token: 0x0600B7AE RID: 47022 RVA: 0x003549CC File Offset: 0x00352BCC
			public float GetSeasonalTemp()
			{
				return this.cachedSeasonalTemp;
			}

			// Token: 0x0600B7AF RID: 47023 RVA: 0x003549D4 File Offset: 0x00352BD4
			public float OutdoorTemperatureAt(int absTick)
			{
				return this.CalculateOutdoorTemperatureAtTile(absTick, true);
			}

			// Token: 0x0600B7B0 RID: 47024 RVA: 0x003549DE File Offset: 0x00352BDE
			public float OffsetFromDailyRandomVariation(int absTick)
			{
				return (float)this.dailyVariationPerlinCached.GetValue((double)absTick, 0.0, 0.0) * 7f;
			}

			// Token: 0x0600B7B1 RID: 47025 RVA: 0x00354A06 File Offset: 0x00352C06
			public float AverageTemperatureForTwelfth(Twelfth twelfth)
			{
				return this.twelfthlyTempAverages[(int)twelfth];
			}

			// Token: 0x0600B7B2 RID: 47026 RVA: 0x00354A10 File Offset: 0x00352C10
			public void CheckCache()
			{
				if (this.tickCachesNeedReset <= Find.TickManager.TicksGame)
				{
					this.tickCachesNeedReset = Find.TickManager.TicksGame + 60;
					Map map = Current.Game.FindMap(this.tile);
					this.cachedOutdoorTemp = this.OutdoorTemperatureAt(Find.TickManager.TicksAbs);
					if (map != null)
					{
						this.cachedOutdoorTemp += AggregateTemperatureOffset(map.gameConditionManager);
					}
					this.cachedSeasonalTemp = this.CalculateOutdoorTemperatureAtTile(Find.TickManager.TicksAbs, false);
				}
			}
			static float AggregateTemperatureOffset(GameConditionManager gameConditionManager)
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

			// Token: 0x0600B7B3 RID: 47027 RVA: 0x00354A9C File Offset: 0x00352C9C
			private float CalculateOutdoorTemperatureAtTile(int absTick, bool includeDailyVariations)
			{
				if (absTick == 0)
				{
					absTick = 1;
				}
				float num = Find.WorldGrid[this.tile].temperature + GenTemperature.OffsetFromSeasonCycle(absTick, this.tile);
				if (includeDailyVariations)
				{
					num += this.OffsetFromDailyRandomVariation(absTick) + GenTemperature.OffsetFromSunCycle(absTick, this.tile);
				}
				return num;
			}

			// Token: 0x04007D33 RID: 32051
			private int tile;

			// Token: 0x04007D34 RID: 32052
			private int tickCachesNeedReset = int.MinValue;

			// Token: 0x04007D35 RID: 32053
			private float cachedOutdoorTemp = float.MinValue;

			// Token: 0x04007D36 RID: 32054
			private float cachedSeasonalTemp = float.MinValue;

			// Token: 0x04007D37 RID: 32055
			private float[] twelfthlyTempAverages;

			// Token: 0x04007D38 RID: 32056
			private Perlin dailyVariationPerlinCached;

			// Token: 0x04007D39 RID: 32057
			private const int CachedTempUpdateInterval = 60;
		}

	}
}

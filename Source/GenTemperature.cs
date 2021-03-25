using HarmonyLib;
using System;
using System.Collections.Generic;
using Verse;
using UnityEngine;

namespace RimThreaded
{

	public class GenTemperature_Patch
    {
		[ThreadStatic] public static List<RoomGroup> neighRoomGroups;

		public static Dictionary<int, float> SeasonalShiftAmplitudeCache = new Dictionary<int, float>();
		public static Dictionary<int, float> tileTemperature = new Dictionary<int, float>();
		public static Dictionary<int, float> absTickOffset = new Dictionary<int, float>();
		public static Dictionary<int, Dictionary<int, float>> tileAbsTickTemperature = new Dictionary<int, Dictionary<int, float>>();

		public static RoomGroup[] beqRoomGroups = AccessTools.StaticFieldRefAccess<RoomGroup[]>(typeof(GenTemperature), "beqRoomGroups");
		
		public static void InitializeThreadStatics()
        {
			neighRoomGroups = new List<RoomGroup>();
		}

		public static void RunDestructivePatches()
        {
			Type original = typeof(GenTemperature);
			Type patched = typeof(GenTemperature_Patch);
			RimThreadedHarmony.Prefix(original, patched, "GetTemperatureFromSeasonAtTile");
			RimThreadedHarmony.Prefix(original, patched, "SeasonalShiftAmplitudeAt");
		}
		
		public static bool SeasonalShiftAmplitudeAt(ref float __result, int tile)
		{
			if (!SeasonalShiftAmplitudeCache.TryGetValue(tile, out __result))
			{
				__result = Find.WorldGrid.LongLatOf(tile).y >= 0.0 ?
					TemperatureTuning.SeasonalTempVariationCurve.Evaluate(Find.WorldGrid.DistanceFromEquatorNormalized(tile)) :
					-TemperatureTuning.SeasonalTempVariationCurve.Evaluate(Find.WorldGrid.DistanceFromEquatorNormalized(tile));
				SeasonalShiftAmplitudeCache[tile] = __result;
			}
			return false;
		}
		public static bool GetTemperatureFromSeasonAtTile(ref float __result, int absTick, int tile)
		{
			if (absTick == 0)
			{
				absTick = 1;
			}
			
			if (!tileAbsTickTemperature.TryGetValue(tile, out Dictionary<int, float> absTickTemperature))
			{
				absTickTemperature = new Dictionary<int, float>();
				tileAbsTickTemperature[tile] = absTickTemperature;
			}
			if (!absTickTemperature.TryGetValue(absTick, out float temperature))
			{
				if (!tileTemperature.TryGetValue(tile, out float temperatureFromTile))
				{
					temperatureFromTile = Find.WorldGrid[tile].temperature;
					tileTemperature[tile] = temperatureFromTile;
				}
				temperature = temperatureFromTile + GenTemperature.OffsetFromSeasonCycle(absTick, tile);
				lock (absTickTemperature)
				{
					absTickTemperature.SetOrAdd(absTick, temperature);
				}
			}
			__result = temperature;
			return false;
		}

	}
}

using System;
using System.Collections.Generic;
using Verse;

namespace RimThreaded
{

	public class GenTemperature_Patch
    {
		[ThreadStatic] public static List<RoomGroup> neighRoomGroups;
		[ThreadStatic] public static RoomGroup[] beqRoomGroups;

		public static Dictionary<int, float> SeasonalShiftAmplitudeCache = new Dictionary<int, float>();
		public static Dictionary<int, float> tileTemperature = new Dictionary<int, float>();
		public static Dictionary<int, Dictionary<int, float>> tileAbsTickTemperature = new Dictionary<int, Dictionary<int, float>>();

		static readonly Type original = typeof(GenTemperature);
		static readonly Type patched = typeof(GenTemperature_Patch);

		public static void InitializeThreadStatics()
        {
			neighRoomGroups = new List<RoomGroup>();
			beqRoomGroups = new RoomGroup[4];
		}

		public static void RunNonDestructivePatches()
        {
			RimThreadedHarmony.AddAllMatchingFields(original, patched);
			RimThreadedHarmony.TranspileFieldReplacements(original, "PushHeat", new[] { typeof(IntVec3), typeof(Map), typeof(float) });
			RimThreadedHarmony.TranspileFieldReplacements(original, "EqualizeTemperaturesThroughBuilding");
		}

		public static void RunDestructivePatches()
        {
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

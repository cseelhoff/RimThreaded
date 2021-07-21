using System;
using System.Collections.Generic;
using RimWorld.Planet;
using Verse;

namespace RimThreaded
{

	public class GenTemperature_Patch
	{
#if RW12
		[ThreadStatic] public static RoomGroup[] beqRoomGroups;
#endif
#if RW13
		[ThreadStatic] public static Room[] beqRooms;
#endif

		public static Dictionary<int, float> SeasonalShiftAmplitudeCache = new Dictionary<int, float>();
		public static Dictionary<int, float> tileTemperature = new Dictionary<int, float>();
		public static Dictionary<int, Dictionary<int, float>> tileAbsTickTemperature = new Dictionary<int, Dictionary<int, float>>();

		static readonly Type original = typeof(GenTemperature);
		static readonly Type patched = typeof(GenTemperature_Patch);
        private static WorldGrid worldGrid;
		public static void InitializeThreadStatics()
		{
#if RW12
			beqRoomGroups = new RoomGroup[4];
#endif
#if RW13
			beqRooms = new Room[4];
#endif

		}

		public static void RunDestructivePatches()
        {
			RimThreadedHarmony.Prefix(original, patched, "GetTemperatureFromSeasonAtTile");
			RimThreadedHarmony.Prefix(original, patched, "SeasonalShiftAmplitudeAt");
		}

		public static bool SeasonalShiftAmplitudeAt(ref float __result, int tile)
        {
            WorldGrid newWorldGrid = Find.WorldGrid;
            if (worldGrid != newWorldGrid)
            {
                worldGrid = newWorldGrid;
                SeasonalShiftAmplitudeCache.Clear();
                tileAbsTickTemperature.Clear();
                tileTemperature.Clear();
#if DEBUG
                Log.Message("RimThreaded is rebuilding WorldGrid Temperature Cache");
#endif
            }

            if (SeasonalShiftAmplitudeCache.TryGetValue(tile, out __result)) return false;
            __result = Find.WorldGrid.LongLatOf(tile).y >= 0.0 ?
                TemperatureTuning.SeasonalTempVariationCurve.Evaluate(newWorldGrid.DistanceFromEquatorNormalized(tile)) :
                -TemperatureTuning.SeasonalTempVariationCurve.Evaluate(newWorldGrid.DistanceFromEquatorNormalized(tile));
            SeasonalShiftAmplitudeCache[tile] = __result;
            return false;
		}
		public static bool GetTemperatureFromSeasonAtTile(ref float __result, int absTick, int tile)
		{
            WorldGrid newWorldGrid = Find.WorldGrid;
            if (worldGrid != newWorldGrid)
            {
                worldGrid = newWorldGrid;
                SeasonalShiftAmplitudeCache.Clear();
                tileAbsTickTemperature.Clear();
                tileTemperature.Clear();
#if DEBUG
				Log.Message("RimThreaded is rebuilding WorldGrid Temperature Cache");
#endif
			}
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

using System;
using System.Collections.Generic;
using Verse;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{
    class RegionTraverser_Transpile
	{
		[ThreadStatic] public static Dictionary<Region, uint[]> regionClosedIndex;
		[ThreadStatic] public static Queue<object> freeWorkers;
		[ThreadStatic] public static int NumWorkers;

		public static void InitializeThreadStatics()
		{
			regionClosedIndex = new Dictionary<Region, uint[]>();
			freeWorkers = new Queue<object>();
			NumWorkers = 8;
			RegionTraverser.RecreateWorkers();
		}

		public static void RunNonDestructivePatches()
		{
			Type original = TypeByName("Verse.RegionTraverser+BFSWorker");
			RimThreadedHarmony.replaceFields.Add(Field(typeof(Region), "closedIndex"), Method(typeof(RegionTraverser_Transpile), "getRegionClosedIndex"));
			RimThreadedHarmony.TranspileFieldReplacements(original, "QueueNewOpenRegion");
			RimThreadedHarmony.TranspileFieldReplacements(original, "BreadthFirstTraverseWork");

			original = typeof(RegionTraverser);
			Type patched = typeof(RegionTraverser_Transpile);
			RimThreadedHarmony.replaceFields.Add(Field(original, "NumWorkers"), Field(patched, "NumWorkers"));
			RimThreadedHarmony.replaceFields.Add(Field(original, "freeWorkers"), Field(patched, "freeWorkers"));
			
			RimThreadedHarmony.TranspileFieldReplacements(original, "BreadthFirstTraverse", new Type[] {
				typeof(Region),
				typeof(RegionEntryPredicate),
				typeof(RegionProcessor),
				typeof(int),
				typeof(RegionType)
			});
			RimThreadedHarmony.TranspileFieldReplacements(original, "RecreateWorkers");
			
		}
		public static uint[] getRegionClosedIndex(Region region)
		{
			if (!regionClosedIndex.TryGetValue(region, out uint[] closedIndex))
			{
				closedIndex = new uint[8];
				regionClosedIndex[region] = closedIndex;
			}
			return closedIndex;
		}
	}
}

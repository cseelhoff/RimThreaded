using System;
using System.Collections.Generic;
using Verse;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{
    class RegionTraverser_Transpile
	{
		[ThreadStatic] public static Dictionary<Region, uint[]> regionClosedIndex;
		[ThreadStatic] public static Queue<RegionTraverser.BFSWorker> freeWorkers;
		[ThreadStatic] public static int NumWorkers;

		public static void InitializeThreadStatics()
		{
			regionClosedIndex = new Dictionary<Region, uint[]>();
            NumWorkers = 8;
			freeWorkers = new Queue<RegionTraverser.BFSWorker>();
			RegionTraverser.RecreateWorkers();
		}

		public static void RunNonDestructivePatches()
		{
			Type original = typeof(RegionTraverser.BFSWorker);
            Type patched = typeof(RegionTraverser_Transpile);
			RimThreadedHarmony.AddAllMatchingFields(original, patched);
			RimThreadedHarmony.replaceFields.Add(Field(typeof(Region), "closedIndex"), Method(typeof(RegionTraverser_Transpile), "GetRegionClosedIndex"));
			RimThreadedHarmony.TranspileFieldReplacements(original, "QueueNewOpenRegion");
			RimThreadedHarmony.TranspileFieldReplacements(original, "BreadthFirstTraverseWork");

			original = typeof(RegionTraverser);
			//RimThreadedHarmony.replaceFields.Add(Field(original, "NumWorkers"), Field(patched, "NumWorkers"));
			//RimThreadedHarmony.replaceFields.Add(Field(original, "freeWorkers"), Field(patched, "freeWorkers"));
			
			RimThreadedHarmony.TranspileFieldReplacements(original, "BreadthFirstTraverse", new [] {
				typeof(Region),
				typeof(RegionEntryPredicate),
				typeof(RegionProcessor),
				typeof(int),
				typeof(RegionType)
			});
			RimThreadedHarmony.TranspileFieldReplacements(original, "RecreateWorkers");
			
		}
		public static uint[] GetRegionClosedIndex(Region region)
		{
            if (regionClosedIndex.TryGetValue(region, out uint[] closedIndex)) return closedIndex;
            closedIndex = new uint[8];
            regionClosedIndex[region] = closedIndex;
            return closedIndex;
		}
	}
}

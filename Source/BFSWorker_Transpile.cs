using System;
using Verse;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{
    class BFSWorker_Transpile
	{
		public static void RunNonDestructivePatches()
        {
			Type original = TypeByName("Verse.RegionTraverser+BFSWorker");
			RimThreadedHarmony.replaceFields.Add(Field(typeof(Region), "closedIndex"), Method(typeof(BFSWorker_Patch2), "getRegionClosedIndex"));
			RimThreadedHarmony.TranspileFieldReplacements(original, "QueueNewOpenRegion");
			RimThreadedHarmony.TranspileFieldReplacements(original, "BreadthFirstTraverseWork");
		}
	}
}

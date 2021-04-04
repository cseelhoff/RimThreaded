using System;
using System.Collections.Generic;
using Verse;

namespace RimThreaded
{
    public class RegionListersUpdater_Patch
    {
		[ThreadStatic] public static List<Region> tmpRegions;

		public static void InitializeThreadStatics()
        {
			tmpRegions = new List<Region>();
		}

		public static void RunNonDestructivePatches()
        {
			Type original = typeof(RegionListersUpdater);
			Type patched = typeof(RegionListersUpdater_Patch);
			RimThreadedHarmony.AddAllMatchingFields(original, patched);
			RimThreadedHarmony.TranspileFieldReplacements(original, "RegisterInRegions");
			RimThreadedHarmony.TranspileFieldReplacements(original, "DeregisterInRegions");
		}
	}
}
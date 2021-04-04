using System;
using System.Collections.Generic;
using Verse.AI;

namespace RimThreaded
{
    class ThinkNode_PrioritySorter_Patch
	{
		[ThreadStatic] public static List<ThinkNode> workingNodes;

		public static void InitializeThreadStatics()
        {
			workingNodes = new List<ThinkNode>();
		}
		internal static void RunNonDestructivePatches()
		{
			Type original = typeof(ThinkNode_PrioritySorter);
			Type patched = typeof(ThinkNode_PrioritySorter_Patch);
			RimThreadedHarmony.AddAllMatchingFields(original, patched);
			RimThreadedHarmony.TranspileFieldReplacements(original, "TryIssueJobPackage");
		}

    }
}

using System;
using System.Collections.Generic;
using Verse.Sound;

namespace RimThreaded
{

	public class SoundSizeAggregator_Patch
	{
		public static bool RegisterReporter(SoundSizeAggregator __instance, ISizeReporter newRep)
		{
			lock (__instance.reporters)
			{
				__instance.reporters.Add(newRep);
			}
			return false;
		}

		public static bool RemoveReporter(SoundSizeAggregator __instance, ISizeReporter oldRep)
		{
			lock (__instance.reporters)
			{
				List<ISizeReporter> newReporters = new List<ISizeReporter>(__instance.reporters);
				newReporters.Remove(oldRep);
				__instance.reporters = newReporters;
			}
			return false;
		}
		public static bool get_AggregateSize(SoundSizeAggregator __instance, ref float __result)
		{
			if (__instance.reporters.Count == 0)
			{
				__result = __instance.testSize;
				return false;
			}

			float num = 0f;
			for (int i = 0; i < __instance.reporters.Count; i++)
			{
				ISizeReporter reporter = __instance.reporters[i];
				if (reporter != null)
				{
					num += reporter.CurrentSize();
				}
			}

			__result = num;
			return false;
		}

        internal static void RunDestructivePatches()
        {
			Type original = typeof(SoundSizeAggregator);
			Type patched = typeof(SoundSizeAggregator_Patch);
			RimThreadedHarmony.Prefix(original, patched, "RegisterReporter");
			RimThreadedHarmony.Prefix(original, patched, "RemoveReporter");
			RimThreadedHarmony.Prefix(original, patched, "get_AggregateSize");
		}
    }
}
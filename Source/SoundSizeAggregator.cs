using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace RimThreaded
{

    public class SoundSizeAggregator_Patch
	{
		public static AccessTools.FieldRef<SoundSizeAggregator, List<ISizeReporter>> reporters =
			AccessTools.FieldRefAccess<SoundSizeAggregator, List<ISizeReporter>>("reporters");
		public static bool RegisterReporter(SoundSizeAggregator __instance, ISizeReporter newRep)
		{
			lock (reporters(__instance))
			{
				reporters(__instance).Add(newRep);
			}
			return false;
		}

		public static bool RemoveReporter(SoundSizeAggregator __instance, ISizeReporter oldRep)
		{
			lock (reporters(__instance))
			{
				reporters(__instance).Remove(oldRep);
			}
			return false;
		}

	}
}

using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using UnityEngine;
using System.Threading;

namespace RimThreaded
{
    public class SubSustainer_Patch
	{
		public static AccessTools.FieldRef<SubSustainer, float> nextSampleStartTime =
			AccessTools.FieldRefAccess<SubSustainer, float>("nextSampleStartTime");

		public static AccessTools.FieldRef<SubSustainer, List<SampleSustainer>> samples =
			AccessTools.FieldRefAccess<SubSustainer, List<SampleSustainer>>("samples");
		private static void EndSample2(SubSustainer __instance, SampleSustainer samp)
		{
			samples(__instance).Remove(samp);
			samp.SampleCleanup();
		}

		public static bool SubSustainerUpdate(SubSustainer __instance)
		{
			for (int index = samples(__instance).Count - 1; index >= 0; --index)
			{
				if ((double)Time.realtimeSinceStartup > (double)samples(__instance)[index].scheduledEndTime)
					EndSample2(__instance, samples(__instance)[index]);
			}
			if ((double)Time.realtimeSinceStartup > (double)nextSampleStartTime(__instance))
				StartSample(__instance);
			for (int index = 0; index < samples(__instance).Count; ++index)
			{
				samples(__instance)[index].Update();
			}
			return false;
		}

		public static bool StartSample(SubSustainer __instance)
		{
			
			ResolvedGrain resolvedGrain = __instance.subDef.RandomizedResolvedGrain();
			if (resolvedGrain == null)
			{
				Log.Error(string.Concat(new object[]
				{
					"SubSustainer for ",
					__instance.subDef,
					" of ",
					__instance.parent.def,
					" could not resolve any grains."
				}), false);
				__instance.parent.End();
				return false;
			}
			float num;
			if (__instance.subDef.sustainLoop)
			{
				num = __instance.subDef.sustainLoopDurationRange.RandomInRange;
			}
			else
			{
				num = resolvedGrain.duration;
			}
			float num2 = Time.realtimeSinceStartup + num;
			nextSampleStartTime(__instance) = num2 + __instance.subDef.sustainIntervalRange.RandomInRange;
			if (nextSampleStartTime(__instance) < Time.realtimeSinceStartup + 0.01f)
			{
				nextSampleStartTime(__instance) = Time.realtimeSinceStartup + 0.01f;
			}
			if (resolvedGrain is ResolvedGrain_Silence)
			{
				return false;
			}

			int tID = Thread.CurrentThread.ManagedThreadId;
			if (RimThreaded.tryMakeAndPlayWaits.TryGetValue(tID, out EventWaitHandle eventWaitStart))
			{
				RimThreaded.tryMakeAndPlayRequests.TryAdd(tID, new object[] { __instance, ((ResolvedGrain_Clip)resolvedGrain).clip, num2 });
				RimThreaded.mainThreadWaitHandle.Set();
				eventWaitStart.WaitOne();
			}			
			return false;
		}


	}
}

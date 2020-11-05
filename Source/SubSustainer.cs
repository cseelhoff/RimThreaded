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
			lock (__instance)
			{
				SampleSustainer sample;
				for (int num = samples(__instance).Count - 1; num >= 0; num--)
				{
					sample = samples(__instance)[num];
					if (sample != null)
					{
						if (Time.realtimeSinceStartup > samples(__instance)[num].scheduledEndTime)
						{
							EndSample2(__instance, sample);
						}
					}
				}

				if (Time.realtimeSinceStartup > nextSampleStartTime(__instance))
				{
					StartSample(__instance);
				}

				for (int i = 0; i < samples(__instance).Count; i++)
				{
					sample = samples(__instance)[i];
					if (sample != null)
					{
						sample.Update();
					}
				}
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

			SampleSustainer sampleSustainer = SampleSustainer.TryMakeAndPlay(__instance, ((ResolvedGrain_Clip)resolvedGrain).clip, num2);
			if (sampleSustainer != null)
			{
				if (__instance.subDef.sustainSkipFirstAttack && Time.frameCount == __instance.creationFrame)
				{
					sampleSustainer.resolvedSkipAttack = true;
				}
				samples(__instance).Add(sampleSustainer);
			}
			/*
			int tID = Thread.CurrentThread.ManagedThreadId;
			if (RimThreaded.mainRequestWaits.TryGetValue(tID, out EventWaitHandle eventWaitStart))
			{
				RimThreaded.tryMakeAndPlayRequests.TryAdd(tID, new object[] { __instance, ((ResolvedGrain_Clip)resolvedGrain).clip, num2 });
				RimThreaded.mainThreadWaitHandle.Set();
				eventWaitStart.WaitOne();
			}
			*/
			return false;
		}


	}
}

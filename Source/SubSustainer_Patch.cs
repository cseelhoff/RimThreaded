using HarmonyLib;
using System;
using System.Collections.Generic;
using Verse;
using Verse.Sound;

namespace RimThreaded
{
    public class SubSustainer_Patch
	{
		internal static void RunDestructivePatches()
		{
			Type original = typeof(SubSustainer);
			Type patched = typeof(SubSustainer_Patch);
			RimThreadedHarmony.Prefix(original, patched, "SubSustainerUpdate");
		}

		private static void EndSample2(SubSustainer __instance, SampleSustainer samp)
		{
			List<SampleSustainer> newSamples = new List<SampleSustainer>(__instance.samples);
			newSamples.Remove(samp);
			__instance.samples = newSamples;
			samp.SampleCleanup();
		}

		public static bool SubSustainerUpdate(SubSustainer __instance)
		{
			lock (__instance)
			{
				SampleSustainer sample;
				for (int num = __instance.samples.Count - 1; num >= 0; num--)
				{
					sample = __instance.samples[num];
					if (sample != null)
					{
						if (Time_Patch.get_realtimeSinceStartup() > __instance.samples[num].scheduledEndTime)
						{
							EndSample2(__instance, sample);
						}
					}
				}

				if (Time_Patch.get_realtimeSinceStartup() > __instance.nextSampleStartTime)
				{
					StartSample(__instance);
				}

				for (int i = 0; i < __instance.samples.Count; i++)
				{
					sample = __instance.samples[i];
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
			float num2 = Time_Patch.get_realtimeSinceStartup() + num;
			__instance.nextSampleStartTime = num2 + __instance.subDef.sustainIntervalRange.RandomInRange;
			if (__instance.nextSampleStartTime < Time_Patch.get_realtimeSinceStartup() + 0.01f)
			{
				__instance.nextSampleStartTime = Time_Patch.get_realtimeSinceStartup() + 0.01f;
			}
			if (resolvedGrain is ResolvedGrain_Silence)
			{
				return false;
			}

			SampleSustainer sampleSustainer = SampleSustainer.TryMakeAndPlay(__instance, ((ResolvedGrain_Clip)resolvedGrain).clip, num2);
			if (sampleSustainer != null)
			{
				if (__instance.subDef.sustainSkipFirstAttack && Time_Patch.get_frameCount() == __instance.creationFrame)
				{
					sampleSustainer.resolvedSkipAttack = true;
				}
				lock (__instance)
				{
					__instance.samples.Add(sampleSustainer);
				}
			}
			return false;
		}

    }
}

using RimWorld.Planet;
using System;
using Verse;
using Verse.Sound;
using static RimThreaded.RimThreaded;
using static System.Threading.Thread;

namespace RimThreaded
{
	class SoundStarter_Patch
	{
		public static bool PlayOneShot(SoundDef soundDef, SoundInfo info)
		{
			if (UnityData.IsInMainThread)
			{
				return true;
			}

			if (soundDef == null)
			{
				Log.Error("Tried to PlayOneShot with null SoundDef. Info=" + info, false);
				return false;
			}
			DebugSoundEventsLog.Notify_SoundEvent(soundDef, info);
			if (soundDef == null)
			{
				return false;
			}
			if (soundDef.isUndefined)
			{
				return false;
			}
			if (soundDef.sustain)
			{
				Log.Error("Tried to play sustainer SoundDef " + soundDef + " as a one-shot sound.", false);
				return false;
			}
			for (int i = 0; i < soundDef.subSounds.Count; i++)
			{
				RimThreaded.PlayOneShot.Enqueue(new Tuple<SoundDef, SoundInfo>(soundDef, info));
			}
			// Don't know why but if this is set to false, threads will hang and timeout.
			return true;
		}

		public static bool PlayOneShotOnCamera(SoundDef soundDef, Map onlyThisMap)
		{
			if (UnityData.IsInMainThread)
			{
				return true;
			}

			if (onlyThisMap != null && (Find.CurrentMap != onlyThisMap || WorldRendererUtility.WorldRenderedNow))
			{
				return false;
			}
			if (soundDef == null)
			{
				return false;
			}

			PlayOneShotCamera.Enqueue(new Tuple<SoundDef, Map>(soundDef, onlyThisMap));
			return false;
		}


		static readonly Func<object[], object> safeFunction = parameters =>
			SoundStarter.TrySpawnSustainer(
				(SoundDef)parameters[0], 
				(SoundInfo)parameters[1]);

		public static bool TrySpawnSustainer(ref Sustainer __result, SoundDef soundDef, SoundInfo info)
		{
			if (allThreads2.TryGetValue(CurrentThread, out ThreadInfo threadInfo))
			{
				threadInfo.safeFunctionRequest = new object[] { safeFunction, new object[] { soundDef, info } };
				mainThreadWaitHandle.Set();
				threadInfo.eventWaitStart.WaitOne();
				__result = (Sustainer)threadInfo.safeFunctionResult;
				return false;
			}
			return true;
		}

        internal static void RunDestructivePatches()
        {
			Type original = typeof(SoundStarter);
			Type patched = typeof(SoundStarter_Patch);
			RimThreadedHarmony.Prefix(original, patched, "PlayOneShot");
			RimThreadedHarmony.Prefix(original, patched, "PlayOneShotOnCamera");
		}
    }
}
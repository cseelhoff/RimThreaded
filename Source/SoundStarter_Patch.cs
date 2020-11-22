using RimWorld.Planet;
using System;
using System.Threading;
using Verse;
using Verse.Sound;

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

			RimThreaded.PlayOneShotCamera.Enqueue(new Tuple<SoundDef, Map>(soundDef, onlyThisMap));
			return false;
		}


		static readonly Func<object[], object> safeFunction = p =>
			SoundStarter.TrySpawnSustainer((SoundDef)p[0], (SoundInfo)p[1]);

		public static bool TrySpawnSustainer(ref Sustainer __result, SoundDef soundDef, SoundInfo info)
		{
			int tID = Thread.CurrentThread.ManagedThreadId;
			if (RimThreaded.mainRequestWaits.TryGetValue(tID, out EventWaitHandle eventWaitStart))
			{
				object[] functionAndParameters = new object[] { safeFunction, new object[] { soundDef, info } };
				lock (RimThreaded.safeFunctionRequests)
				{
					RimThreaded.safeFunctionRequests[tID] = functionAndParameters;
				}
				RimThreaded.mainThreadWaitHandle.Set();
				eventWaitStart.WaitOne();
				RimThreaded.safeFunctionResults.TryGetValue(tID, out object safeFunctionResult);
				__result = (Sustainer)safeFunctionResult;
				return false;
			}
			return true;
		}

	}
}
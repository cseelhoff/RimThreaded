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
				Ticklist_Patch.PlayOneShot.Enqueue(new Tuple<SoundDef, SoundInfo>(soundDef, info));
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

			Ticklist_Patch.PlayOneShotCamera.Enqueue(new Tuple<SoundDef, Map>(soundDef, onlyThisMap));
			return false;
		}
		public static bool TrySpawnSustainer(ref Sustainer __result, SoundDef soundDef, SoundInfo info)
		{
			DebugSoundEventsLog.Notify_SoundEvent(soundDef, info);
			if (soundDef == null)
			{
				__result = null;
				return false;
			}
			if (soundDef.isUndefined)
			{
				__result = null;
				return false;
			}
			if (!soundDef.sustain)
			{
				Log.Error("Tried to spawn a sustainer from non-sustainer sound " + soundDef + ".", false);
				__result = null;
				return false;
			}
			if (!info.IsOnCamera && info.Maker.Thing != null && info.Maker.Thing.Destroyed)
			{
				__result = null;
				return false;
			}
			if (soundDef.sustainStartSound != null)
			{
				soundDef.sustainStartSound.PlayOneShot(info);
			}
			Sustainer sustainer = null;
			int tID = Thread.CurrentThread.ManagedThreadId;
			if (Ticklist_Patch.newSustainerWaits.TryGetValue(tID, out EventWaitHandle eventWaitStart))
			{
				Ticklist_Patch.newSustainerRequests.TryAdd(tID, new object[] { soundDef, info });
				Ticklist_Patch.mainThreadWaitHandle.Set();
				eventWaitStart.WaitOne();

				Ticklist_Patch.newSustainerResults.TryGetValue(tID, out sustainer);
				__result = sustainer;
			} else
            {
				__result = new Sustainer(soundDef, info);
			}
			//return new Sustainer(soundDef, info);
			//return sustainer;
			return false;

		}
	}
}
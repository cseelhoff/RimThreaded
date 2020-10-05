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

    public class AudioSourceMaker_Patch
	{
        public static bool NewAudioSourceOn(ref AudioSource __result, GameObject go)
        {
            int tID = Thread.CurrentThread.ManagedThreadId;
            if (RimThreaded.newAudioSourceWaits.TryGetValue(tID, out EventWaitHandle eventWaitStart))
            {
                RimThreaded.newAudioSourceRequests.TryAdd(tID, go);
                RimThreaded.mainThreadWaitHandle.Set();
                eventWaitStart.WaitOne();
                RimThreaded.newAudioSourceResults.TryGetValue(tID, out AudioSource newAudioSource_result);
                __result = newAudioSource_result;
                return false;
            }
            return true;
        }

    }
}

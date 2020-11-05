using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using System.Threading;
using UnityEngine;

namespace RimThreaded
{

    public class SampleSustainer_Patch
	{

        public static bool TryMakeAndPlay(ref SampleSustainer __result, SubSustainer subSus, AudioClip clip, float scheduledEndTime)
        {
            int tID = Thread.CurrentThread.ManagedThreadId;
            if (RimThreaded.mainRequestWaits.TryGetValue(tID, out EventWaitHandle eventWaitStart))
            {
                lock (RimThreaded.tryMakeAndPlayRequests)
                {
                    RimThreaded.tryMakeAndPlayRequests[tID] = new object[] { subSus, clip, scheduledEndTime };
                }
                RimThreaded.mainThreadWaitHandle.Set();
                eventWaitStart.WaitOne();
                if(!RimThreaded.tryMakeAndPlayResults.TryGetValue(tID, out SampleSustainer sustainer_result))
                {
                    Log.Error("Error retriving tryMakeAndPlayResults");
                }
                __result = sustainer_result;
                return false;
            }
            return true;
        }



    }
}

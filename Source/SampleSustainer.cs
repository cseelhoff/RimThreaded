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

        static readonly Func<object[], object> safeFunction = p =>
            SampleSustainer.TryMakeAndPlay((SubSustainer)p[0], (AudioClip)p[1], (float)p[2]);

        public static bool TryMakeAndPlay(ref SampleSustainer __result, SubSustainer subSus, AudioClip clip, float scheduledEndTime)
        {
            int tID = Thread.CurrentThread.ManagedThreadId;
            if (RimThreaded.mainRequestWaits.TryGetValue(tID, out EventWaitHandle eventWaitStart))
            {
                object[] functionAndParameters = new object[] { safeFunction, new object[] { subSus, clip, scheduledEndTime } };
                lock (RimThreaded.safeFunctionRequests)
                {
                    RimThreaded.safeFunctionRequests[tID] = functionAndParameters;
                }
                RimThreaded.mainThreadWaitHandle.Set();
                eventWaitStart.WaitOne();
                RimThreaded.safeFunctionResults.TryGetValue(tID, out object safeFunctionResult);
                __result = (SampleSustainer)safeFunctionResult;
                return false;
            }
            return true;
        }


    }
}

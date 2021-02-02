using System;
using Verse.Sound;
using UnityEngine;
using static RimThreaded.RimThreaded;
using static System.Threading.Thread;

namespace RimThreaded
{

    public class SampleSustainer_Patch
	{

        static readonly Func<object[], object> safeFunction = parameters =>
            SampleSustainer.TryMakeAndPlay(
                (SubSustainer)parameters[0], 
                (AudioClip)parameters[1], 
                (float)parameters[2]);

        public static bool TryMakeAndPlay(ref SampleSustainer __result, SubSustainer subSus, AudioClip clip, float scheduledEndTime)
        {
            if (allThreads2.TryGetValue(CurrentThread, out ThreadInfo threadInfo))
            {
                threadInfo.safeFunctionRequest = new object[] { safeFunction, new object[] { subSus, clip, scheduledEndTime } };
                mainThreadWaitHandle.Set();
                threadInfo.eventWaitStart.WaitOne();
                __result = (SampleSustainer)threadInfo.safeFunctionResult;
                return false;
            }
            return true;
        }


    }
}

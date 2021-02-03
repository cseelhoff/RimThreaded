using System;
using Verse.Sound;
using UnityEngine;
using static System.Threading.Thread;
using static RimThreaded.RimThreaded;

namespace RimThreaded
{

    public class AudioSourceMaker_Patch
	{
        static readonly Func<object[], object> safeFunction = parameters =>
            AudioSourceMaker.NewAudioSourceOn((GameObject)parameters[0]);

        public static bool NewAudioSourceOn(ref AudioSource __result, GameObject go)
        {
            if (allThreads2.TryGetValue(CurrentThread, out ThreadInfo threadInfo))
            {
                threadInfo.safeFunctionRequest = new object[] { safeFunction, new object[] { go } };
                mainThreadWaitHandle.Set();
                threadInfo.eventWaitStart.WaitOne();
                __result = (AudioSource)threadInfo.safeFunctionResult;
                return false;
            }
            return true;
        }

    }
}

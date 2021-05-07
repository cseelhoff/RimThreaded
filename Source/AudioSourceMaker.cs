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

        public static void RunDestructivePatches()
        {
            Type original = typeof(AudioSourceMaker);
            Type patched = typeof(AudioSourceMaker_Patch);
            RimThreadedHarmony.Prefix(original, patched, "NewAudioSourceOn");
        }

        public static bool NewAudioSourceOn(ref AudioSource __result, GameObject go)
        {
            if (!CurrentThread.IsBackground || !allThreads2.TryGetValue(CurrentThread, out ThreadInfo threadInfo)) 
                return true;
            threadInfo.safeFunctionRequest = new object[] { safeFunction, new object[] { go } };
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            __result = (AudioSource)threadInfo.safeFunctionResult;
            return false;
        }

    }
}

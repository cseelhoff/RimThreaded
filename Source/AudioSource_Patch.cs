using System;
using System.Reflection;
using UnityEngine;
using static HarmonyLib.AccessTools;
using static RimThreaded.RimThreaded;
using static System.Threading.Thread;

namespace RimThreaded
{
    class AudioSource_Patch
    {
        static readonly MethodInfo methodAudioSourceStop = Method(typeof(AudioSource), "Stop", Type.EmptyTypes);

        static readonly Action<AudioSource> actionAudioSourceStop =
            (Action<AudioSource>)Delegate.CreateDelegate(typeof(Action<AudioSource>), methodAudioSourceStop);

        public static bool Stop(AudioSource __instance)
        {
            if (allThreads2.TryGetValue(CurrentThread, out ThreadInfo threadInfo))
            {
                threadInfo.safeFunctionRequest = new object[] { actionAudioSourceStop, new object[] { __instance } };
                mainThreadWaitHandle.Set();
                threadInfo.eventWaitStart.WaitOne();
                return false;
            }
            return true;
        }

    }
}

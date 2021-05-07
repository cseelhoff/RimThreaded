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

        public static void RunDestructivePatches()
        {
            Type original = typeof(AudioSource);
            Type patched = typeof(AudioSource_Patch);
            RimThreadedHarmony.Prefix(original, patched, "Stop", Type.EmptyTypes);
        }

        public static bool Stop(AudioSource __instance)
        {
            if (!CurrentThread.IsBackground || !allThreads2.TryGetValue(CurrentThread, out ThreadInfo threadInfo)) 
                return true;
            threadInfo.safeFunctionRequest = new object[] { actionAudioSourceStop, new object[] { __instance } };
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return false;
        }

    }
}

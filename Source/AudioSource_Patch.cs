using System;
using UnityEngine;
using static HarmonyLib.AccessTools;
using static RimThreaded.RimThreaded;
using static System.Threading.Thread;

namespace RimThreaded
{
    class AudioSource_Patch
    {
        private static readonly Action<AudioSource> ActionAudioSourceStop =
            (Action<AudioSource>)Delegate.CreateDelegate(typeof(Action<AudioSource>), 
                Method(typeof(AudioSource), "Stop", Type.EmptyTypes));

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
            threadInfo.safeFunctionRequest = new object[] { ActionAudioSourceStop, new object[] { __instance } };
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return false;
        }

    }
}

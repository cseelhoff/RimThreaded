using System;
using UnityEngine;
using static HarmonyLib.AccessTools;
using static System.Threading.Thread;
using static RimThreaded.RimThreaded;

namespace RimThreaded
{
    class AudioHighPassFilter_Patch
    {

        private static readonly Action<AudioHighPassFilter, float> ActionSetHighpassResonanceQ =
            (Action<AudioHighPassFilter, float>)Delegate.CreateDelegate(
                typeof(Action<AudioHighPassFilter, float>),
                Method(typeof(AudioHighPassFilter), "set_highpassResonanceQ"));

        private static readonly Action<object[]> ActionSetHighpassResonanceQ2 = parameters =>
            ActionSetHighpassResonanceQ((AudioHighPassFilter)parameters[0], (float)parameters[1]);

        public static void set_highpassResonanceQ(AudioHighPassFilter __instance, float value)
        {
            if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out ThreadInfo threadInfo))
            {
                __instance.highpassResonanceQ = value;
                return;
            }
            threadInfo.safeFunctionRequest = new object[] { ActionSetHighpassResonanceQ2, new object[] { __instance, value } };
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return;
        }

        private static readonly Action<AudioHighPassFilter, float> ActionSetCutoffFrequency =
            (Action<AudioHighPassFilter, float>)Delegate.CreateDelegate(
                typeof(Action<AudioHighPassFilter, float>),
                Method(typeof(AudioHighPassFilter), "set_cutoffFrequency"));

        private static readonly Action<object[]> ActionSetCutoffFrequency2 = parameters =>
            ActionSetCutoffFrequency((AudioHighPassFilter)parameters[0], (float)parameters[1]);

        public static void set_cutoffFrequency(AudioHighPassFilter __instance, float value)
        {
            if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out ThreadInfo threadInfo))
            {
                __instance.cutoffFrequency = value;
                return;
            }
            threadInfo.safeFunctionRequest = new object[] { ActionSetCutoffFrequency2, new object[] { __instance, value } };
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return;
        }
    }
}

using System;
using UnityEngine;
using static HarmonyLib.AccessTools;
using static System.Threading.Thread;
using static RimThreaded.RimThreaded;

namespace RimThreaded
{
    class AudioLowPassFilter_Patch
    {

        private static readonly Action<AudioLowPassFilter, float> ActionSetLowpassResonanceQ =
            (Action<AudioLowPassFilter, float>)Delegate.CreateDelegate(
                typeof(Action<AudioLowPassFilter, float>),
                Method(typeof(AudioLowPassFilter), "set_lowpassResonanceQ"));

        private static readonly Action<object[]> ActionSetLowpassResonanceQ2 = parameters =>
            ActionSetLowpassResonanceQ((AudioLowPassFilter)parameters[0], (float)parameters[1]);

        public static void set_lowpassResonanceQ(AudioLowPassFilter __instance, float value)
        {
            if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out ThreadInfo threadInfo))
            {
                __instance.lowpassResonanceQ = value;
                return;
            }
            if (__instance is AudioLowPassFilter lowPassFilter)
            {
                threadInfo.safeFunctionRequest = new object[] { ActionSetLowpassResonanceQ2, new object[] { lowPassFilter, value } };
                mainThreadWaitHandle.Set();
                threadInfo.eventWaitStart.WaitOne();
            }
            return;
        }

        private static readonly Action<AudioLowPassFilter, float> ActionSetCutoffFrequency =
            (Action<AudioLowPassFilter, float>)Delegate.CreateDelegate(
                typeof(Action<AudioLowPassFilter, float>),
                Method(typeof(AudioLowPassFilter), "set_cutoffFrequency"));

        private static readonly Action<object[]> ActionSetCutoffFrequency2 = parameters =>
            ActionSetCutoffFrequency((AudioLowPassFilter)parameters[0], (float)parameters[1]);

        public static void set_cutoffFrequency(AudioLowPassFilter __instance, float value)
        {
            if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out ThreadInfo threadInfo))
            {
                __instance.cutoffFrequency = value;
                return;
            }
            if (__instance is AudioLowPassFilter lowPassFilter)
            {
                //Action<object[]> ActionSetCutoffFrequency3 = parameters => __instance.cutoffFrequency = value;
                threadInfo.safeFunctionRequest = new object[] { ActionSetCutoffFrequency2, new object[] { lowPassFilter, value } };
                //threadInfo.safeFunctionRequest = new object[] { ActionSetCutoffFrequency3, new object[] { __instance, value } };
                mainThreadWaitHandle.Set();
                threadInfo.eventWaitStart.WaitOne();
            }
            return;
        }
    }
}

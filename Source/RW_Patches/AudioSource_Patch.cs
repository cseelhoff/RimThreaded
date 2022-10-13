using System;
using System.Reflection;
using UnityEngine;
using static HarmonyLib.AccessTools;
using static RimThreaded.RimThreaded;
using static System.Threading.Thread;

namespace RimThreaded.RW_Patches
{
    class AudioSource_Patch
    {
        private static readonly Action<AudioSource> ActionAudioSourceStop =
            (Action<AudioSource>)Delegate.CreateDelegate(typeof(Action<AudioSource>),
                Method(typeof(AudioSource), nameof(Stop), Type.EmptyTypes));

        public static void RunDestructivePatches()
        {
            Type original = typeof(AudioSource);
            Type patched = typeof(AudioSource_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(Stop), Type.EmptyTypes);
        }

        public static bool Stop(AudioSource __instance)
        {
            if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out ThreadInfo threadInfo))
                return true;
            threadInfo.safeFunctionRequest = new object[] { ActionAudioSourceStop, new object[] { __instance } };
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return false;
        }



        public static float get_volume(AudioSource __instance)
        {
            if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out ThreadInfo threadInfo))
            {
                return __instance.volume;
            }
            Func<object[], object> FuncGetVolume = parameters => __instance.volume;
            threadInfo.safeFunctionRequest = new object[] { FuncGetVolume, new object[] { __instance } };
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return (float)threadInfo.safeFunctionResult;
        }



        private static readonly Action<AudioSource, bool> ActionSetLoop =
            (Action<AudioSource, bool>)Delegate.CreateDelegate(
                typeof(Action<AudioSource, bool>),
                Method(typeof(AudioSource), nameof(set_loop)));

        private static readonly Action<object[]> ActionSetLoop2 = parameters =>
            ActionSetLoop((AudioSource)parameters[0], (bool)parameters[1]);

        public static void set_loop(AudioSource __instance, bool value)
        {
            if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out ThreadInfo threadInfo))
            {
                __instance.loop = value;
                return;
            }
            threadInfo.safeFunctionRequest = new object[] { ActionSetLoop2, new object[] { __instance, value } };
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return;
        }


        private static readonly Action<AudioSource> ActionPlay =
            (Action<AudioSource>)Delegate.CreateDelegate(
                typeof(Action<AudioSource>),
                Method(typeof(AudioSource), nameof(Play), Type.EmptyTypes));

        private static readonly Action<object[]> ActionPlay2 = parameters =>
            ActionPlay((AudioSource)parameters[0]);

        public static void Play(AudioSource __instance)
        {
            if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out ThreadInfo threadInfo))
            {
                __instance.Play();
                return;
            }
            threadInfo.safeFunctionRequest = new object[] { ActionPlay2, new object[] { __instance } };
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return;
        }


        private static readonly Action<AudioSource, bool> ActionSetMute =
            (Action<AudioSource, bool>)Delegate.CreateDelegate(
                typeof(Action<AudioSource, bool>),
                Method(typeof(AudioSource), nameof(set_mute)));

        private static readonly Action<object[]> ActionSetMute2 = parameters =>
            ActionSetMute((AudioSource)parameters[0], (bool)parameters[1]);

        public static void set_mute(AudioSource __instance, bool value)
        {
            if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out ThreadInfo threadInfo))
            {
                __instance.mute = value;
                return;
            }
            threadInfo.safeFunctionRequest = new object[] { ActionSetMute2, new object[] { __instance, value } };
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return;
        }


        private static readonly Action<AudioSource, float> ActionSetSpatialBlend =
            (Action<AudioSource, float>)Delegate.CreateDelegate(
                typeof(Action<AudioSource, float>),
                Method(typeof(AudioSource), nameof(set_spatialBlend)));

        private static readonly Action<object[]> ActionSetSpatialBlend2 = parameters =>
            ActionSetSpatialBlend((AudioSource)parameters[0], (float)parameters[1]);

        public static void set_spatialBlend(AudioSource __instance, float value)
        {
            if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out ThreadInfo threadInfo))
            {
                __instance.spatialBlend = value;
                return;
            }
            threadInfo.safeFunctionRequest = new object[] { ActionSetSpatialBlend2, new object[] { __instance, value } };
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return;
        }


        private static readonly Action<AudioSource, float> ActionSetMaxDistance =
            (Action<AudioSource, float>)Delegate.CreateDelegate(
                typeof(Action<AudioSource, float>),
                Method(typeof(AudioSource), nameof(set_maxDistance)));

        private static readonly Action<object[]> ActionSetMaxDistance2 = parameters =>
            ActionSetMaxDistance((AudioSource)parameters[0], (float)parameters[1]);

        public static void set_maxDistance(AudioSource __instance, float value)
        {
            if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out ThreadInfo threadInfo))
            {
                __instance.maxDistance = value;
                return;
            }
            threadInfo.safeFunctionRequest = new object[] { ActionSetMaxDistance2, new object[] { __instance, value } };
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return;
        }


        private static readonly Action<AudioSource, float> ActionSetMinDistance =
            (Action<AudioSource, float>)Delegate.CreateDelegate(
                typeof(Action<AudioSource, float>),
                Method(typeof(AudioSource), nameof(set_minDistance)));

        private static readonly Action<object[]> ActionSetMinDistance2 = parameters =>
            ActionSetMinDistance((AudioSource)parameters[0], (float)parameters[1]);


        public static void set_minDistance(AudioSource __instance, float value)
        {
            if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out ThreadInfo threadInfo))
            {
                __instance.minDistance = value;
                return;
            }
            threadInfo.safeFunctionRequest = new object[] { ActionSetMinDistance2, new object[] { __instance, value } };
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return;
        }


        private static readonly MethodInfo AudioSourceSetPitch =
            Method(typeof(AudioSource), nameof(set_pitch));

        private static readonly Action<AudioSource, float> ActionSetPitch =
            (Action<AudioSource, float>)Delegate.CreateDelegate(
                typeof(Action<AudioSource, float>),
                AudioSourceSetPitch);

        private static readonly Action<object[]> ActionSetPitch2 = parameters =>
            ActionSetPitch((AudioSource)parameters[0], (float)parameters[1]);

        public static void set_pitch(AudioSource __instance, float value)
        {
            if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out ThreadInfo threadInfo))
            {
                __instance.pitch = value;
                return;
            }
            threadInfo.safeFunctionRequest = new object[] { ActionSetPitch2, new object[] { __instance, value } };
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return;
        }


        private static readonly MethodInfo AudioSourceSetVolume =
            Method(typeof(AudioSource), nameof(set_volume));

        private static readonly Action<AudioSource, float> ActionSet_volume =
            (Action<AudioSource, float>)Delegate.CreateDelegate(
                typeof(Action<AudioSource, float>),
                AudioSourceSetVolume);

        private static readonly Action<object[]> ActionSet_volume2 = parameters =>
            ActionSet_volume((AudioSource)parameters[0], (float)parameters[1]);

        public static void set_volume(AudioSource __instance, float value)
        {
            if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out ThreadInfo threadInfo))
            {
                __instance.volume = value;
                return;
            }
            threadInfo.safeFunctionRequest = new object[] { ActionSet_volume2, new object[] { __instance, value } };
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return;
        }

        private static readonly MethodInfo AudioSourceSetClip =
            Method(typeof(AudioSource), nameof(set_clip));

        private static readonly Action<AudioSource, AudioClip> ActionSet_clip =
            (Action<AudioSource, AudioClip>)Delegate.CreateDelegate(
                typeof(Action<AudioSource, AudioClip>),
                AudioSourceSetClip);

        private static readonly Action<object[]> ActionSet_clip2 = parameters =>
            ActionSet_clip((AudioSource)parameters[0], (AudioClip)parameters[1]);

        public static void set_clip(AudioSource __instance, AudioClip value)
        {
            if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out ThreadInfo threadInfo))
            {
                __instance.clip = value;
                return;
            }
            threadInfo.safeFunctionRequest = new object[] { ActionSet_clip2, new object[] { __instance, value } };
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return;
        }

    }
}

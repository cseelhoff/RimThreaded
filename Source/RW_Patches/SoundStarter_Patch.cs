using System;
using Verse;
using Verse.Sound;
using static RimThreaded.RimThreaded;
using static System.Threading.Thread;

namespace RimThreaded.RW_Patches
{
    class SoundStarter_Patch
    {
        internal static void RunDestructivePatches()
        {
            Type original = typeof(SoundStarter);
            Type patched = typeof(SoundStarter_Patch);
            RimThreadedHarmony.Prefix(original, patched, "PlayOneShot");
            RimThreadedHarmony.Prefix(original, patched, "PlayOneShotOnCamera");
        }

        static readonly Action<object[]> ActionPlayOneShot = parameters =>
            ((SoundDef)parameters[0]).PlayOneShot(
                (SoundInfo)parameters[1]);

        public static bool PlayOneShot(SoundDef soundDef, SoundInfo info)
        {
            if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out ThreadInfo threadInfo))
                return true;
            threadInfo.safeFunctionRequest = new object[] { ActionPlayOneShot, new object[] { soundDef, info } };
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return false;
        }
        static readonly Action<object[]> ActionPlayOneShotOnCamera = parameters =>
            ((SoundDef)parameters[0]).PlayOneShotOnCamera(
                (Map)parameters[1]);

        public static bool PlayOneShotOnCamera(SoundDef soundDef, Map onlyThisMap)
        {
            if (!CurrentThread.IsBackground || !allWorkerThreads.TryGetValue(CurrentThread, out ThreadInfo threadInfo))
                return true;
            threadInfo.safeFunctionRequest = new object[] { ActionPlayOneShotOnCamera, new object[] { soundDef, onlyThisMap } };
            mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return false;
        }


    }
}
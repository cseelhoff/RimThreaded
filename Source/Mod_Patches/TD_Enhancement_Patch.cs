using System;
using Verse;
using static HarmonyLib.AccessTools;
using System.Threading;

namespace RimThreaded.Mod_Patches
{
    class TD_Enchancement_Patch
    {
        public static ReaderWriterLockSlim learnedInfo_Lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
        public static void Patch()
        {
            Type TD_Enhancement_Pack_Learn_Patch = TypeByName("TD_Enhancement_Pack.Learn_Patch");
            if (TD_Enhancement_Pack_Learn_Patch != null)
            {
                string methodName = "Postfix";
                Log.Message("RimThreaded is patching " + TD_Enhancement_Pack_Learn_Patch.FullName + " " + methodName);
                RimThreadedHarmony.Prefix(TD_Enhancement_Pack_Learn_Patch, typeof(TD_Enchancement_Patch), methodName, destructive: false, PatchMethod: nameof(WriterPrefix), finalizer: nameof(WriterFinalizer));
            }
            Type LearnedGameComponent = TypeByName("TD_Enhancement_Pack.LearnedGameComponent");
            if (LearnedGameComponent != null)
            {
                string methodName = "GameComponentTick";
                Log.Message("RimThreaded is patching " + LearnedGameComponent.FullName + " " + methodName);
                RimThreadedHarmony.Prefix(LearnedGameComponent, typeof(TD_Enchancement_Patch), methodName, destructive: false, PatchMethod: nameof(WriterPrefix), finalizer: nameof(WriterFinalizer));
            }

        }
        public static void WriterPrefix()
        {
            learnedInfo_Lock.EnterWriteLock();
        }
        public static void WriterFinalizer()
        {
            learnedInfo_Lock.ExitWriteLock();
        }
    }
}

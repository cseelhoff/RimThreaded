using System;
using System.Collections.Generic;
using System.Threading;
using RimWorld;
using Verse;

namespace RimThreaded.RW_Patches
{
    class IdeoManager_Patch
    {
        internal static void RunNonDestructivePatches()//there may be the need for locks in the IdeoManager
        {
            Type original = typeof(IdeoManager);
        }
        public static List<Ideo> Ideos;
        public static int IdeosCount;

        public static void IdeosPrepare()
        {
            Ideos = Current.Game.World.ideoManager.ideos;
            IdeosCount = Ideos.Count;
        }
        public static void IdeosTick()
        {
            while (true)
            {
                int index = Interlocked.Decrement(ref IdeosCount);
                if (index < 0) return;
                try
                {
                    Ideos[index].IdeoTick();
                }
                catch (Exception e)
                {
                    Log.Error("Exception ticking Ideo: " + Ideos[index].ToString() + ": " + e);
                }
            }
        }
    }
}
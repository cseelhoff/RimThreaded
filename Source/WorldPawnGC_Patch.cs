using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{
    class WorldPawnGC_Patch
    {
        public static bool WorldPawnGCTick(WorldPawnGC __instance)
        {
            if (__instance.lastSuccessfulGCTick >= Find.TickManager.TicksGame / 15000 * 15000)
            {
                return false;
            }

            //if (activeGCProcess == null)
            //{
                //activeGCProcess = PawnGCPass().GetEnumerator();
                if (DebugViewSettings.logWorldPawnGC)
                {
                    Log.Message($"World pawn GC started at rate {__instance.currentGCRate}");
                }
            //}

            //if (activeGCProcess == null)
            //{
                //return false;
            //}

            bool flag = false;
            for (int i = 0; i < __instance.currentGCRate; i++)
            {
                if (flag)
                {
                    break;
                }

                //flag = !activeGCProcess.MoveNext();
            }

            if (flag)
            {
                __instance.lastSuccessfulGCTick = Find.TickManager.TicksGame;
                __instance.currentGCRate = 1;
                //activeGCProcess = null;
                if (DebugViewSettings.logWorldPawnGC)
                {
                    Log.Message("World pawn GC complete");
                }
            }
            return false;
        }
    }
}

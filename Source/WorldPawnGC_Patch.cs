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

        private static readonly FieldRef<WorldPawnGC, int> lastSuccessfulGCTickFieldRef = FieldRefAccess<WorldPawnGC, int>("lastSuccessfulGCTick");
        private static readonly FieldRef<WorldPawnGC, int> currentGCRateFieldRef = FieldRefAccess<WorldPawnGC, int>("currentGCRate");

        public static bool WorldPawnGCTick(WorldPawnGC __instance)
        {
            if (lastSuccessfulGCTickFieldRef(__instance) >= Find.TickManager.TicksGame / 15000 * 15000)
            {
                return false;
            }

            //if (activeGCProcess == null)
            //{
                //activeGCProcess = PawnGCPass().GetEnumerator();
                if (DebugViewSettings.logWorldPawnGC)
                {
                    Log.Message($"World pawn GC started at rate {currentGCRateFieldRef(__instance)}");
                }
            //}

            //if (activeGCProcess == null)
            //{
                //return false;
            //}

            bool flag = false;
            for (int i = 0; i < currentGCRateFieldRef(__instance); i++)
            {
                if (flag)
                {
                    break;
                }

                //flag = !activeGCProcess.MoveNext();
            }

            if (flag)
            {
                lastSuccessfulGCTickFieldRef(__instance) = Find.TickManager.TicksGame;
                currentGCRateFieldRef(__instance) = 1;
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

using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace RimThreaded
{
    class ThinkNode_ConditionalAnyColonistTryingToExitMap_Patch
    {
        public static bool Satisfied(ThinkNode_ConditionalAnyColonistTryingToExitMap __instance, ref bool __result, Pawn pawn)
        {
            Map mapHeld = pawn.MapHeld;
            if (mapHeld == null)
            {
                __result = false;
                return false;
            }

            List<Pawn> freeColonistsSpawned = mapHeld.mapPawns.FreeColonistsSpawned;
            for (int i = freeColonistsSpawned.Count; i >= 0; i--)
            {
                Pawn item;
                try
                {
                    item = freeColonistsSpawned[i];
                } catch(ArgumentOutOfRangeException)
                {
                    break;
                }
                if (item != null)
                {
                    Job curJob = item.CurJob;
                    if (curJob != null && curJob.exitMapOnArrival)
                    {
                        __result = true;
                        return false;
                    }
                }
            }

            __result = false;
            return false;
        }
    }
}

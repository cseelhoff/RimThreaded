using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace RimThreaded.RW_Patches
{
    public class RecipeWorkerCounter_Patch
    {
        internal static void RunDestructivePatches()
        {
            Type original = typeof(RecipeWorkerCounter);
            Type patched = typeof(RecipeWorkerCounter_Patch);
            RimThreadedHarmony.Prefix(original, patched, "GetCarriedCount");
        }

        public static bool GetCarriedCount(RecipeWorkerCounter __instance, ref int __result, Bill_Production bill, ThingDef prodDef)
        {
            int num = 0;
            //foreach (Pawn item in bill.Map.mapPawns.FreeColonistsSpawned)
            if (!RimThreaded.billFreeColonistsSpawned.TryGetValue(bill, out List<Pawn> freeColonistsSpawned))
            {
                freeColonistsSpawned = bill.Map.mapPawns.FreeColonistsSpawned;
                RimThreaded.billFreeColonistsSpawned[bill] = freeColonistsSpawned;
            }
            for (int i = 0; i < freeColonistsSpawned.Count; i++)
            {
                Thing carriedThing = freeColonistsSpawned[i]?.carryTracker?.CarriedThing;
                if (carriedThing == null) continue;
                int stackCount = carriedThing.stackCount;
                carriedThing = carriedThing.GetInnerIfMinified();
                if (__instance.CountValidThing(carriedThing, bill, prodDef))
                {
                    num += stackCount;
                }
            }

            __result = num;
            return false;
        }

    }
}

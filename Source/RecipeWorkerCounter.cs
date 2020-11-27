using RimWorld;
using System;
using Verse;

public class RecipeWorkerCounter_Patch
{
    public static bool GetCarriedCount(RecipeWorkerCounter __instance, ref int __result, Bill_Production bill, ThingDef prodDef)
    {
        int num = 0;
        //foreach (Pawn item in bill.Map.mapPawns.FreeColonistsSpawned)
        for (int i = 0; i < bill.Map.mapPawns.FreeColonistsSpawned.Count; i++)
        {
            Pawn item;
            try
            {
                item = bill.Map.mapPawns.FreeColonistsSpawned[i];
            } catch(ArgumentOutOfRangeException)
            {
                break;
            }
            Thing carriedThing = item.carryTracker.CarriedThing;
            if (carriedThing != null)
            {
                int stackCount = carriedThing.stackCount;
                carriedThing = carriedThing.GetInnerIfMinified();
                if (__instance.CountValidThing(carriedThing, bill, prodDef))
                {
                    num += stackCount;
                }
            }
        }

        __result = num;
        return false;
    }
}

using RimWorld;
using System;
using Verse;
using Verse.AI;

namespace RimThreaded
{
    class JobGiver_Haul_Patch
    {
        public static bool TryGiveJob(ref Job __result, Pawn pawn)
        {
            int validationChecks = 0;
            int validatorFalses1 = 0;
            int validatorFalses2 = 0;
            int validatorFalses3 = 0;
            int validatorFalses4 = 0;
            Predicate<Thing> validator = delegate (Thing t)
            {
                validationChecks++;
                if (t.IsForbidden(pawn))
                {
                    validatorFalses1++;
                    return false;
                }

                if (!HaulAIUtility.PawnCanAutomaticallyHaulFast(pawn, t, forced: false))
                {
                    validatorFalses2++;
                    return false;
                }

                if (pawn.carryTracker.MaxStackSpaceEver(t.def) <= 0)
                {
                    validatorFalses3++;
                    return false;
                }

                IntVec3 foundCell;
                bool tryFindBestBetterStoreCellFor = StoreUtility.TryFindBestBetterStoreCellFor(t, pawn, pawn.Map, StoreUtility.CurrentStoragePriorityOf(t), pawn.Faction, out foundCell) ? true : false;
                if (!tryFindBestBetterStoreCellFor)
                    validatorFalses4++;
                return tryFindBestBetterStoreCellFor;
            };
            //Log.Error(pawn.Map.listerHaulables.ThingsPotentiallyNeedingHauling().Count.ToString());
            Thing thing = GenClosest.ClosestThing_Global_Reachable(pawn.Position, pawn.Map, pawn.Map.listerHaulables.ThingsPotentiallyNeedingHauling(), PathEndMode.OnCell, TraverseParms.For(pawn), 9999f, validator);
            if (validationChecks > 10)
                Log.Error("Validator Checks: " + validationChecks.ToString() + " " + validatorFalses1.ToString() + " " + validatorFalses2.ToString() + " " + validatorFalses3.ToString() + " " + validatorFalses4.ToString() + " ");
            if (thing != null)
            {
                __result = HaulAIUtility.HaulToStorageJob(pawn, thing);
                return false;
            }
            __result = null;
            return false;
        }
    }
}

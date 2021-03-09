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
            Predicate<Thing> validator = delegate (Thing t)
            {
                if (t.IsForbidden(pawn))
                {
                    return false;
                }

                if (!HaulAIUtility.PawnCanAutomaticallyHaulFast(pawn, t, forced: false))
                {
                    return false;
                }

                if (pawn.carryTracker.MaxStackSpaceEver(t.def) <= 0)
                {
                    return false;
                }

                IntVec3 foundCell;
                return StoreUtility.TryFindBestBetterStoreCellFor(t, pawn, pawn.Map, StoreUtility.CurrentStoragePriorityOf(t), pawn.Faction, out foundCell) ? true : false;
            };
            //Log.Error(pawn.Map.listerHaulables.ThingsPotentiallyNeedingHauling().Count.ToString());
            Thing thing = GenClosest.ClosestThing_Global_Reachable(pawn.Position, pawn.Map, pawn.Map.listerHaulables.ThingsPotentiallyNeedingHauling(), PathEndMode.OnCell, TraverseParms.For(pawn), 9999f, validator);
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

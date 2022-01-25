using RimWorld;
using System;
using Verse;
using Verse.AI;

namespace RimThreaded
{
    class JobGiver_ExitMap_Patch
    {
        internal static void RunDestructivePatches()
        {
            Type original = typeof(JobGiver_ExitMap);
            Type patched = typeof(JobGiver_ExitMap_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(TryGiveJob));
        }

        public static bool TryGiveJob(JobGiver_ExitMap __instance, ref Job __result, Pawn pawn)
        {
            Pawn_MindState mindState = pawn.mindState;
            bool canDig = __instance.forceCanDig || 
                (pawn.mindState.duty != null && pawn.mindState.duty.canDig && !pawn.CanReachMapEdge()) ||
                (__instance.forceCanDigIfCantReachMapEdge && !pawn.CanReachMapEdge()) ||
                (__instance.forceCanDigIfAnyHostileActiveThreat && pawn.Faction != null && GenHostility.AnyHostileActiveThreatTo_NewTemp(pawn.Map, pawn.Faction, countDormantPawnsAsHostile: true));
            IntVec3 dest;
            if (!__instance.TryFindGoodExitDest(pawn, canDig, out dest))
            {
                __result = null;
                return false;
            }
            if (canDig)
            {
                using (PawnPath path = pawn.Map.pathFinder.FindPath(pawn.Position, dest, TraverseParms.For(pawn, mode: TraverseMode.PassAllDestroyableThings)))
                {
                    IntVec3 cellBefore;
                    Thing blocker = path.FirstBlockingBuilding(out cellBefore, pawn);
                    if (blocker != null)
                    {
                        Job job = DigUtility.PassBlockerJob(pawn, blocker, cellBefore, true, true);
                        if (job != null)
                        {
                            __result = job;
                            return false;
                        }
                    }
                }
            }
            Job job1 = JobMaker.MakeJob(JobDefOf.Goto, dest);
            job1.exitMapOnArrival = true;
            job1.failIfCantJoinOrCreateCaravan = __instance.failIfCantJoinOrCreateCaravan;
            job1.locomotionUrgency = PawnUtility.ResolveLocomotion(pawn, __instance.defaultLocomotion, LocomotionUrgency.Jog);
            job1.expiryInterval = __instance.jobMaxDuration;
            job1.canBashDoors = __instance.canBash;
            __result = job1;
            return false;
        }
    }
}

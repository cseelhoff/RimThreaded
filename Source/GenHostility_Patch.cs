using System;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimThreaded
{
    class GenHostility_Patch
    {
        public static void RunDestructivePatches()
        {
            Type original = typeof(GenHostility);
            Type patched = typeof(GenHostility_Patch);
            RimThreadedHarmony.Prefix(original, patched, "HostileTo", new Type[] {typeof(Thing), typeof(Thing)});
        }
		public static bool HostileTo(ref bool __result, Thing a, Thing b)
        {
            if (a == null || b == null)
            {
                __result = false;
                return false;
            }
            if (a.Destroyed || b.Destroyed || a == b)
            {
                __result = false;
                return false;
			}
			if ((a.Faction == null && a.TryGetComp<CompCauseGameCondition>() != null) || (b.Faction == null && b.TryGetComp<CompCauseGameCondition>() != null))
			{
                __result = true;
                return false;
			}
			Pawn pawn = a as Pawn;
			Pawn pawn2 = b as Pawn;
            __result = (pawn?.MentalState != null && pawn.MentalState.ForceHostileTo(b)) || 
                   (pawn2?.MentalState != null && pawn2.MentalState.ForceHostileTo(a)) || 
                   (pawn != null && pawn2 != null && (IsPredatorHostileTo(pawn, pawn2) || 
                                                      IsPredatorHostileTo(pawn2, pawn))) || 
                   ((a.Faction != null && pawn2 != null && pawn2.HostFaction == a.Faction && (pawn?.HostFaction == null) && PrisonBreakUtility.IsPrisonBreaking(pawn2)) || 
                    (b.Faction != null && pawn != null && pawn.HostFaction == b.Faction && (pawn2?.HostFaction == null) && PrisonBreakUtility.IsPrisonBreaking(pawn))) || 
                   ((a.Faction == null || 
                     pawn2 == null || 
                     pawn2.HostFaction != a.Faction) && (b.Faction == null || 
                                                         pawn == null ||
                                                         pawn.HostFaction != b.Faction) && (pawn == null || 
                       !pawn.IsPrisoner || 
                       pawn2 == null || 
                       !pawn2.IsPrisoner) && (pawn == null || 
                                              pawn2 == null || 
                                              ((!pawn.IsPrisoner || 
                                                pawn.HostFaction != pawn2.HostFaction || 
                                                PrisonBreakUtility.IsPrisonBreaking(pawn)) && (!pawn2.IsPrisoner || 
                                                  pawn2.HostFaction != pawn.HostFaction || 
                                                  PrisonBreakUtility.IsPrisonBreaking(pawn2)))) && (pawn == null || 
                       pawn2 == null || 
                       ((pawn.HostFaction == null || 
                         pawn2.Faction == null || 
                         pawn.HostFaction.HostileTo(pawn2.Faction) || 
                         PrisonBreakUtility.IsPrisonBreaking(pawn)) && (pawn2.HostFaction == null || 
                                                                        pawn.Faction == null || 
                                                                        pawn2.HostFaction.HostileTo(pawn.Faction) || 
                                                                        PrisonBreakUtility.IsPrisonBreaking(pawn2)))) && (a.Faction == null || 
                       !a.Faction.IsPlayer || 
                       pawn2 == null || 
                       !pawn2.mindState.WillJoinColonyIfRescued) && (b.Faction == null || 
                                                                     !b.Faction.IsPlayer || 
                                                                     pawn == null || 
                                                                     !pawn.mindState.WillJoinColonyIfRescued) && ((pawn != null && pawn.Faction == null && pawn.RaceProps.Humanlike && b.Faction != null && b.Faction.def.hostileToFactionlessHumanlikes) || 
                       (pawn2 != null && pawn2.Faction == null && pawn2.RaceProps.Humanlike && a.Faction != null && a.Faction.def.hostileToFactionlessHumanlikes) || 
                       (a.Faction != null && b.Faction != null && a.Faction.HostileTo(b.Faction))));
            return false;
        }
        private static bool IsPredatorHostileTo(Pawn predator, Pawn toPawn)
        {
            if (toPawn.Faction == null)
            {
                return false;
            }
            if (toPawn.Faction.HasPredatorRecentlyAttackedAnyone(predator))
            {
                return true;
            }
            Pawn preyOfMyFaction = GetPreyOfMyFaction(predator, toPawn.Faction);
            return preyOfMyFaction != null && predator.Position.InHorDistOf(preyOfMyFaction.Position, 12f);
        }
        private static Pawn GetPreyOfMyFaction(Pawn predator, Faction myFaction)
        {
            Job curJob = predator.CurJob;
            if (curJob != null && curJob.def == JobDefOf.PredatorHunt && !predator.jobs.curDriver.ended)
            {
                Pawn pawn = curJob.GetTarget(TargetIndex.A).Thing as Pawn;
                if (pawn != null && !pawn.Dead && pawn.Faction == myFaction)
                {
                    return pawn;
                }
            }
            return null;
        }
	}
}
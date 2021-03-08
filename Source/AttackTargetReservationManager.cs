using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using static Verse.AI.AttackTargetReservationManager;

namespace RimThreaded
{

    public class AttackTargetReservationManager_Patch
	{
		public static AccessTools.FieldRef<AttackTargetReservationManager, List<AttackTargetReservation>> reservations =
			AccessTools.FieldRefAccess<AttackTargetReservationManager, List<AttackTargetReservation>>("reservations");
		public static AccessTools.FieldRef<AttackTargetReservationManager, Map> map =
			AccessTools.FieldRefAccess<AttackTargetReservationManager, Map>("map");

		public static bool ReleaseAllForTarget(AttackTargetReservationManager __instance, IAttackTarget target)
		{
			lock (RimThreaded.map_AttackTargetReservationManager_reservations_Lock)
			{
				List<AttackTargetReservation> snapshotReservations = reservations(__instance);
				List<AttackTargetReservation> newAttackTargetReservations = new List<AttackTargetReservation>(snapshotReservations.Count);
				for (int i = 0; i < snapshotReservations.Count - 1; i++)
				{
					AttackTargetReservation attackTargetReservation = snapshotReservations[i];				
					if (attackTargetReservation.target != target)
					{
						newAttackTargetReservations.Add(attackTargetReservation);
					}
				}
				reservations(__instance) = newAttackTargetReservations;
			}
			
			return false;
		}

		public static bool ReleaseAllClaimedBy(AttackTargetReservationManager __instance, Pawn claimant)
		{
			lock (RimThreaded.map_AttackTargetReservationManager_reservations_Lock)
			{
				List<AttackTargetReservation> snapshotReservations = reservations(__instance);
				List<AttackTargetReservation> newAttackTargetReservations = new List<AttackTargetReservation>(snapshotReservations.Count);
				for (int i = 0; i < snapshotReservations.Count - 1; i++)
				{
					AttackTargetReservation attackTargetReservation = snapshotReservations[i];
					if (attackTargetReservation.claimant != claimant)
					{
						newAttackTargetReservations.Add(attackTargetReservation);
					}
				}
				reservations(__instance) = newAttackTargetReservations;
			}
			return false;
		}


		public static bool Reserve(AttackTargetReservationManager __instance, Pawn claimant, Job job, IAttackTarget target)
		{
			if (target == null)
			{
				Log.Warning(string.Concat(claimant, " tried to reserve null attack target."));
			}
			else if (!__instance.IsReservedBy(claimant, target))
			{
                AttackTargetReservation attackTargetReservation = new AttackTargetReservation
                {
                    target = target,
                    claimant = claimant,
                    job = job
                };
                lock (RimThreaded.map_AttackTargetReservationManager_reservations_Lock)
				{
					reservations(__instance) = new List<AttackTargetReservation>(reservations(__instance))
                    {
                        attackTargetReservation
                    };
				}
			}
			return false;
		}

		public static bool FirstReservationFor(AttackTargetReservationManager __instance, ref IAttackTarget __result, Pawn claimant)
		{
            List<AttackTargetReservation> snapshotReservations = reservations(__instance);
			for (int i = snapshotReservations.Count - 1; i >= 0; i--)
			{
                AttackTargetReservation reservation = snapshotReservations[i];
				if (reservation.claimant == claimant)
				{
					__result = reservation.target;
					return false;
				}
			}			
			__result = null;
			return false;
		}
		public static bool ReleaseClaimedBy(AttackTargetReservationManager __instance, Pawn claimant, Job job)
		{
			lock (RimThreaded.map_AttackTargetReservationManager_reservations_Lock)
			{
				List<AttackTargetReservation> snapshotReservations = reservations(__instance);
				List<AttackTargetReservation> newAttackTargetReservations = new List<AttackTargetReservation>(snapshotReservations.Count);
				for (int i = 0; i < snapshotReservations.Count - 1; i++)
				{
					AttackTargetReservation attackTargetReservation = snapshotReservations[i];
					if (attackTargetReservation.claimant != claimant || attackTargetReservation.job != job)
					{
						newAttackTargetReservations.Add(attackTargetReservation);
					}
				}
				reservations(__instance) = newAttackTargetReservations;
			}
			return false;
		}
		public static bool IsReservedBy(AttackTargetReservationManager __instance, ref bool __result, Pawn claimant, IAttackTarget target)
		{
			List<AttackTargetReservation> snapshotReservations = reservations(__instance);
			for (int i = 0; i < snapshotReservations.Count; i++)
			{
				AttackTargetReservation attackTargetReservation = snapshotReservations[i];
				if (attackTargetReservation.target == target && attackTargetReservation.claimant == claimant)
				{
					__result = true;
					return false;
				}
			}

			__result = false;
			return false;
		}

		public static bool GetReservationsCount(AttackTargetReservationManager __instance, ref int __result, IAttackTarget target, Faction faction)
		{
			int num = 0;
			List<AttackTargetReservation> snapshotReservations = reservations(__instance);
			for (int i = 0; i < snapshotReservations.Count; i++)
			{
				AttackTargetReservation attackTargetReservation = snapshotReservations[i];
				if (attackTargetReservation.target == target && attackTargetReservation.claimant.Faction == faction)
				{
					num++;
				}
			}

			__result = num;
			return false;
		}
	}
}

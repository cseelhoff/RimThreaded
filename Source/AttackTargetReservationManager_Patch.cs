using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using static Verse.AI.AttackTargetReservationManager;

namespace RimThreaded
{

    public class AttackTargetReservationManager_Patch
	{
		internal static void RunDestructivePatches()
		{
			Type original = typeof(AttackTargetReservationManager);
			Type patched = typeof(AttackTargetReservationManager_Patch);
			RimThreadedHarmony.Prefix(original, patched, "FirstReservationFor");
			RimThreadedHarmony.Prefix(original, patched, "ReleaseClaimedBy");
			RimThreadedHarmony.Prefix(original, patched, "ReleaseAllForTarget");
			RimThreadedHarmony.Prefix(original, patched, "ReleaseAllClaimedBy");
			RimThreadedHarmony.Prefix(original, patched, "GetReservationsCount");
			RimThreadedHarmony.Prefix(original, patched, "Reserve");
			RimThreadedHarmony.Prefix(original, patched, "IsReservedBy");
		}

		public static bool ReleaseAllForTarget(AttackTargetReservationManager __instance, IAttackTarget target)
		{
			lock (RimThreaded.map_AttackTargetReservationManager_reservations_Lock)
			{
				List<AttackTargetReservation> snapshotReservations = __instance.reservations;
				List<AttackTargetReservation> newAttackTargetReservations = new List<AttackTargetReservation>(snapshotReservations.Count);
				for (int i = 0; i < snapshotReservations.Count - 1; i++)
				{
					AttackTargetReservation attackTargetReservation = snapshotReservations[i];				
					if (attackTargetReservation.target != target)
					{
						newAttackTargetReservations.Add(attackTargetReservation);
					}
				}
                __instance.reservations = newAttackTargetReservations;
			}
			
			return false;
		}

		public static bool ReleaseAllClaimedBy(AttackTargetReservationManager __instance, Pawn claimant)
		{
			lock (RimThreaded.map_AttackTargetReservationManager_reservations_Lock)
			{
				List<AttackTargetReservation> snapshotReservations = __instance.reservations;
				List<AttackTargetReservation> newAttackTargetReservations = new List<AttackTargetReservation>(snapshotReservations.Count);
				for (int i = 0; i < snapshotReservations.Count - 1; i++)
				{
					AttackTargetReservation attackTargetReservation = snapshotReservations[i];
					if (attackTargetReservation.claimant != claimant)
					{
						newAttackTargetReservations.Add(attackTargetReservation);
					}
				}
                __instance.reservations = newAttackTargetReservations;
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
                    __instance.reservations = new List<AttackTargetReservation>(__instance.reservations)
                    {
                        attackTargetReservation
                    };
				}
			}
			return false;
		}

		public static bool FirstReservationFor(AttackTargetReservationManager __instance, ref IAttackTarget __result, Pawn claimant)
		{
            List<AttackTargetReservation> snapshotReservations = __instance.reservations;
			for (int i = snapshotReservations.Count - 1; i >= 0; i--)
			{
                AttackTargetReservation reservation = snapshotReservations[i];
                if (reservation.claimant != claimant) continue;
                __result = reservation.target;
                return false;
            }			
			__result = null;
			return false;
		}
		public static bool ReleaseClaimedBy(AttackTargetReservationManager __instance, Pawn claimant, Job job)
		{
			lock (RimThreaded.map_AttackTargetReservationManager_reservations_Lock)
			{
				List<AttackTargetReservation> snapshotReservations = __instance.reservations;
				List<AttackTargetReservation> newAttackTargetReservations = new List<AttackTargetReservation>(snapshotReservations.Count);
				for (int i = 0; i < snapshotReservations.Count - 1; i++)
				{
					AttackTargetReservation attackTargetReservation = snapshotReservations[i];
					if (attackTargetReservation.claimant != claimant || attackTargetReservation.job != job)
					{
						newAttackTargetReservations.Add(attackTargetReservation);
					}
				}
                __instance.reservations = newAttackTargetReservations;
			}
			return false;
		}
		public static bool IsReservedBy(AttackTargetReservationManager __instance, ref bool __result, Pawn claimant, IAttackTarget target)
		{
			List<AttackTargetReservation> snapshotReservations = __instance.reservations;
			for (int i = 0; i < snapshotReservations.Count; i++)
			{
				AttackTargetReservation attackTargetReservation = snapshotReservations[i];
                if (attackTargetReservation.target != target || attackTargetReservation.claimant != claimant) continue;
                __result = true;
                return false;
            }

			__result = false;
			return false;
		}

		public static bool GetReservationsCount(AttackTargetReservationManager __instance, ref int __result, IAttackTarget target, Faction faction)
		{
			int num = 0;
			List<AttackTargetReservation> snapshotReservations = __instance.reservations;
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

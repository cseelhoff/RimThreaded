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
			List<AttackTargetReservation> attackTargetReservations = reservations(__instance);
			lock (attackTargetReservations)
			{
				for (int i = attackTargetReservations.Count - 1; i >= 0; i--)
				{
					AttackTargetReservation attackTargetReservation = attackTargetReservations[i];				
					if (attackTargetReservation != null && attackTargetReservation.target == target)
					{
						attackTargetReservations.RemoveAt(i);
					}
				}
			}
			
			return false;
		}

		public static bool ReleaseAllClaimedBy(AttackTargetReservationManager __instance, Pawn claimant)
		{
			lock (reservations(__instance))
			{
				for (int num = reservations(__instance).Count - 1; num >= 0; num--)
				{
					AttackTargetReservation reservation = reservations(__instance)[num];
					if (reservation != null && reservation.claimant == claimant)
					{
						reservations(__instance).RemoveAt(num);
					}
				}
			}
			return false;
		}
		private static int GetReservationsCount2(AttackTargetReservationManager __instance, IAttackTarget target, Faction faction)
		{
			int num = 0;
			for (int i = 0; i < reservations(__instance).Count; i++)
			{
				AttackTargetReservation attackTargetReservation = reservations(__instance)[i];
				if (attackTargetReservation != null)
				{
					if (attackTargetReservation.target == target && attackTargetReservation.claimant != null && attackTargetReservation.claimant.Faction == faction)
					{
						num++;
					}
                }				
			}

			return num;
		}
		private static int GetMaxPreferredReservationsCount2(AttackTargetReservationManager __instance, IAttackTarget target)
		{
			int num = 0;
			CellRect cellRect = target.Thing.OccupiedRect();
			foreach (IntVec3 item in cellRect.ExpandedBy(1))
			{
				if (!cellRect.Contains(item) && item.InBounds(map(__instance)) && item.Standable(map(__instance)))
				{
					num++;
				}
			}

			return num;
		}


		public static bool CanReserve(AttackTargetReservationManager __instance, ref bool __result, Pawn claimant, IAttackTarget target)
		{
			if (__instance.IsReservedBy(claimant, target))
			{
				__result = true;
				return false;
			}

			int reservationsCount = GetReservationsCount2(__instance, target, claimant.Faction);
			int maxPreferredReservationsCount = GetMaxPreferredReservationsCount2(__instance, target);
			__result = reservationsCount < maxPreferredReservationsCount;
			return false;
		}


		public void Reserve(AttackTargetReservationManager __instance, Pawn claimant, Job job, IAttackTarget target)
		{
			bool isReservedBy = __instance.IsReservedBy(claimant, target);
			if (target == null)
			{
				Log.Warning(claimant + " tried to reserve null attack target.");
			}
			else if (!isReservedBy)
			{
				AttackTargetReservation attackTargetReservation = new AttackTargetReservation();
				attackTargetReservation.target = target;
				attackTargetReservation.claimant = claimant;
				attackTargetReservation.job = job;
				lock (reservations(__instance))
				{
					reservations(__instance).Add(attackTargetReservation);
				}
			}
		}

		public static bool FirstReservationFor(AttackTargetReservationManager __instance, ref IAttackTarget __result, Pawn claimant)
		{
			lock (reservations(__instance))
			{
				for (int i = reservations(__instance).Count - 1; i >= 0; i--)
				{
					if (null != reservations(__instance)[i])
					{
						if (reservations(__instance)[i].claimant == claimant)
						{
							__result = reservations(__instance)[i].target;
							return false;
						}
					}
				}
			}
			__result = null;
			return false;
		}
		public static bool ReleaseClaimedBy(AttackTargetReservationManager __instance, Pawn claimant, Job job)
		{
			for (int i = reservations(__instance).Count - 1; i >= 0; i--)
			{
				if (null != reservations(__instance)[i])
				{
					if (reservations(__instance)[i].claimant == claimant && reservations(__instance)[i].job == job)
					{
						lock (reservations(__instance))
						{
							reservations(__instance).RemoveAt(i);
						}
					}

				}
			}
			return false;
		}
		public static bool IsReservedBy(AttackTargetReservationManager __instance, ref bool __result, Pawn claimant, IAttackTarget target)
		{
			AttackTargetReservation attackTargetReservation;
			for (int i = 0; i < reservations(__instance).Count; i++)
			{
				try
				{
					attackTargetReservation = reservations(__instance)[i];
				}
				catch (ArgumentOutOfRangeException) { break; }
				if (attackTargetReservation != null && attackTargetReservation.target == target && attackTargetReservation.claimant == claimant)
				{
					__result = true;
					return false;
				}
			}
			__result = false;
			return false;
		}
	}
}

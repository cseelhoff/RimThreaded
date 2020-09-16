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
        public static AccessTools.FieldRef<AttackTargetReservationManager, List<AttackTargetReservationManager.AttackTargetReservation>> reservations =
            AccessTools.FieldRefAccess<AttackTargetReservationManager, List<AttackTargetReservationManager.AttackTargetReservation>>("reservations");

		public void Reserve(AttackTargetReservationManager __instance, Pawn claimant, Job job, IAttackTarget target)
		{
			bool isReservedBy = false;
			IsReservedBy(__instance, ref isReservedBy, claimant, target);
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
				} catch (ArgumentOutOfRangeException) { break; }
				if (null != attackTargetReservation)
				{
					if (attackTargetReservation.target == target && attackTargetReservation.claimant == claimant)
					{
						return true;
					}
				}
			}

			return false;
		}

	}
}

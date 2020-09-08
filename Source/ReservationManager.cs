using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using System.Collections.Concurrent;

namespace RimThreaded
{

	public class ReservationManager_Patch
	{
		public static AccessTools.FieldRef<ReservationManager, List<ReservationManager.Reservation>> reservations =
			AccessTools.FieldRefAccess<ReservationManager, List<ReservationManager.Reservation>>("reservations");
		public static AccessTools.FieldRef<ReservationManager, Map> map =
			AccessTools.FieldRefAccess<ReservationManager, Map>("map");

		public static bool ReleaseClaimedBy(ReservationManager __instance, Pawn claimant, Job job)
		{
			lock (reservations(__instance))
			{
				ReservationManager.Reservation[] reservations2 = reservations(__instance).ToArray();
				for (int i = reservations2.Length - 1; i >= 0; i--)
				{
					ReservationManager.Reservation r = reservations2[i];
					if (null != r)
					{
						if (r.Claimant == claimant && r.Job == job)
						{
							reservations(__instance).RemoveAt(i);
						}
					}
				}
			}
			return false;
		}

		public static bool CanReserve(ReservationManager __instance,
		  Pawn claimant,
		  LocalTargetInfo target,
		  int maxPawns = 1,
		  int stackCount = -1,
		  ReservationLayerDef layer = null,
		  bool ignoreOtherReservations = false)
		{
			if (claimant == null)
			{
				Log.Error("CanReserve with null claimant", false);
				return false;
			}
			if (!claimant.Spawned || claimant.Map != map(__instance) || (!target.IsValid || target.ThingDestroyed) || target.HasThing && target.Thing.SpawnedOrAnyParentSpawned && target.Thing.MapHeld != map(__instance))
				return false;
			int num1 = target.HasThing ? target.Thing.stackCount : 1;
			int num2 = stackCount == -1 ? num1 : stackCount;
			if (num2 > num1)
				return false;
			if (!ignoreOtherReservations)
			{
				if (map(__instance).physicalInteractionReservationManager.IsReserved(target) && !map(__instance).physicalInteractionReservationManager.IsReservedBy(claimant, target))
					return false;
				for (int index = 0; index < reservations(__instance).Count; ++index)
				{
					ReservationManager.Reservation reservation = reservations(__instance)[index];
					if (null != reservation)
					{
						if (reservation.Target == target && reservation.Layer == layer && reservation.Claimant == claimant && (reservation.StackCount == -1 || reservation.StackCount >= num2))
							return true;
					}
				}
				int num3 = 0;
				int num4 = 0;
				for (int index = 0; index < reservations(__instance).Count; ++index)
				{
					ReservationManager.Reservation reservation = reservations(__instance)[index];
					if (!(reservation.Target != target) && reservation.Layer == layer && (reservation.Claimant != claimant && RespectsReservationsOf(claimant, reservation.Claimant)))
					{
						if (reservation.MaxPawns != maxPawns)
							return false;
						++num3;
						if (reservation.StackCount == -1)
							num4 += num1;
						else
							num4 += reservation.StackCount;
						if (num3 >= maxPawns || num2 + num4 > num1)
							return false;
					}
				}
			}
			return true;
		}

		private static bool RespectsReservationsOf(Pawn newClaimant, Pawn oldClaimant)
		{
			return newClaimant == oldClaimant || newClaimant.Faction != null && oldClaimant.Faction != null && (newClaimant.Faction == oldClaimant.Faction || !newClaimant.Faction.HostileTo(oldClaimant.Faction) || oldClaimant.HostFaction != null && oldClaimant.HostFaction == newClaimant.HostFaction || newClaimant.HostFaction != null && (oldClaimant.HostFaction != null || newClaimant.HostFaction == oldClaimant.Faction));
		}

		public static bool Reserve(ReservationManager __instance, ref bool __result,
		  Pawn claimant,
		  Job job,
		  LocalTargetInfo target,
		  int maxPawns = 1,
		  int stackCount = -1,
		  ReservationLayerDef layer = null,
		  bool errorOnFailed = true)
		{
			if (maxPawns > 1 && stackCount == -1)
				Log.ErrorOnce("Reserving with maxPawns > 1 and stackCount = All; this will not have a useful effect (suppressing future warnings)", 83269, false);
			if (job == null)
			{
				Log.Warning(claimant.ToStringSafe<Pawn>() + " tried to reserve thing " + target.ToStringSafe<LocalTargetInfo>() + " without a valid job", false);
				__result = false;
				return false;
			}
			int num1 = target.HasThing ? target.Thing.stackCount : 1;
			int num2 = stackCount == -1 ? num1 : stackCount;
			for (int index = 0; index < reservations(__instance).Count; ++index)
			{
				ReservationManager.Reservation reservation = reservations(__instance)[index];
				if (reservation.Target == target && reservation.Claimant == claimant && (reservation.Job == job && reservation.Layer == layer) && (reservation.StackCount == -1 || reservation.StackCount >= num2)) {
					__result = true;
					return false;
				}
			}
			if (!target.IsValid || target.ThingDestroyed)
			{
				__result = false;
				return false;
			}
			if (!CanReserve(__instance, claimant, target, maxPawns, stackCount, layer, false))
			{
				if (job != null && job.playerForced && CanReserve(__instance, claimant, target, maxPawns, stackCount, layer, true))
				{
					reservations(__instance).Add(new ReservationManager.Reservation(claimant, job, maxPawns, stackCount, target, layer));
					foreach (ReservationManager.Reservation reservation in reservations(__instance).ToList<ReservationManager.Reservation>())
					{
						if (reservation.Target == target && reservation.Claimant != claimant && (reservation.Layer == layer && RespectsReservationsOf(claimant, reservation.Claimant)))
							reservation.Claimant.jobs.EndCurrentOrQueuedJob(reservation.Job, JobCondition.InterruptForced, true);
					}
					__result = true;
					return false;
				}
				if (errorOnFailed)
					LogCouldNotReserveError(__instance, claimant, job, target, maxPawns, stackCount, layer);
				__result = false;
				return false;
			}
			reservations(__instance).Add(new ReservationManager.Reservation(claimant, job, maxPawns, stackCount, target, layer));
			__result = true;
			return false;
		}

		private static void LogCouldNotReserveError(ReservationManager __instance,
		  Pawn claimant,
		  Job job,
		  LocalTargetInfo target,
		  int maxPawns,
		  int stackCount,
		  ReservationLayerDef layer)
		{
			Job curJob1 = claimant.CurJob;
			string str1 = "null";
			int num1 = -1;
			if (curJob1 != null)
			{
				str1 = curJob1.ToString();
				if (claimant.jobs.curDriver != null)
					num1 = claimant.jobs.curDriver.CurToilIndex;
			}
			string str2 = !target.HasThing || target.Thing.def.stackLimit == 1 ? "" : "(current stack count: " + (object)target.Thing.stackCount + ")";
			string str3 = "Could not reserve " + target.ToStringSafe<LocalTargetInfo>() + str2 + " (layer: " + layer.ToStringSafe<ReservationLayerDef>() + ") for " + claimant.ToStringSafe<Pawn>() + " for job " + job.ToStringSafe<Job>() + " (now doing job " + str1 + "(curToil=" + (object)num1 + ")) for maxPawns " + (object)maxPawns + " and stackCount " + (object)stackCount + ".";
			Pawn pawn1 = __instance.FirstRespectedReserver(target, claimant);
			string text;
			if (pawn1 != null)
			{
				string str4 = "null";
				int num2 = -1;
				Job curJob2 = pawn1.CurJob;
				if (curJob2 != null)
				{
					str4 = curJob2.ToStringSafe<Job>();
					if (pawn1.jobs.curDriver != null)
						num2 = pawn1.jobs.curDriver.CurToilIndex;
				}
				text = str3 + " Existing reserver: " + pawn1.ToStringSafe<Pawn>() + " doing job " + str4 + " (toilIndex=" + (object)num2 + ")";
			}
			else
				text = str3 + " No existing reserver.";
			Pawn pawn2 = map(__instance).physicalInteractionReservationManager.FirstReserverOf(target);
			if (pawn2 != null)
				text = text + " Physical interaction reserver: " + pawn2.ToStringSafe<Pawn>();
			Log.Error(text, false);
		}

		public static bool FirstReservationFor(ReservationManager __instance, ref LocalTargetInfo __result, Pawn claimant)
		{
			if (claimant == null)
			{
				__result = LocalTargetInfo.Invalid;
				return false;
			}
			ReservationManager.Reservation[] reservations2 = reservations(__instance).ToArray();
			for (int i = 0; i < reservations2.Length; i++)
			{
				ReservationManager.Reservation r = reservations2[i];
				if (null != r)
				{
					if (r.Claimant == claimant)
					{
						__result = r.Target;
						return false;
					}
				}
			}
			__result = LocalTargetInfo.Invalid;
			return false;
		}
	}
}

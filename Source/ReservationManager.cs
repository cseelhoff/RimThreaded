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
using static Verse.AI.ReservationManager;
using UnityEngine;

namespace RimThreaded
{

	public class ReservationManager_Patch
	{
		public static AccessTools.FieldRef<ReservationManager, List<ReservationManager.Reservation>> reservations =
			AccessTools.FieldRefAccess<ReservationManager, List<ReservationManager.Reservation>>("reservations");
		public static AccessTools.FieldRef<ReservationManager, Map> map =
			AccessTools.FieldRefAccess<ReservationManager, Map>("map");

		public static bool IsReservedByAnyoneOf(ReservationManager __instance, ref bool __result, LocalTargetInfo target, Faction faction)
		{
			if (!target.IsValid)
			{
				__result = false;
				return false;
			}
			for (int i = 0; i < reservations(__instance).Count; i++)
			{
                Reservation reservation = reservations(__instance)[i];
				if (reservation != null)
				{
					if (reservation.Target == target && reservation.Claimant.Faction == faction)
					{
						__result = true;
						return false;
					}
				}
			}
			__result = false;
			return false;
		}

		public static bool ReleaseClaimedBy(ReservationManager __instance, Pawn claimant, Job job)
		{
			List<Reservation> reservationList = reservations(__instance);
			//ReservationManager.Reservation[] reservations2 = reservations(__instance).ToArray();
				
			for (int i = reservationList.Count - 1; i >= 0; i--)
			{
				Reservation r;
				try
				{
					r = reservationList[i];
				} catch(ArgumentOutOfRangeException)
                {
					break;
                }
				if (null != r)
				{
					if (r.Claimant == claimant && r.Job == job)
					{
						lock (reservationList)
						{
							if (i < reservationList.Count && r == reservationList[i])
							{
								reservationList.RemoveAt(i);
							} else
                            {
								Log.Warning("Reservation " + r.ToString() + " was not at expected list index when attempting to remove.");
							}
						}
					}
				}
			}
			
			return false;
		}
		public static bool Release(ReservationManager __instance, LocalTargetInfo target, Pawn claimant, Job job)
		{
			if (target.ThingDestroyed)
			{
				Log.Warning("Releasing destroyed thing " + target + " for " + claimant);
			}
            Reservation reservation1 = null;
            Reservation reservation2;
			for (int index = 0; index < reservations(__instance).Count; ++index)
			{
				try
				{
					reservation2 = reservations(__instance)[index];
				} catch (ArgumentOutOfRangeException) { break; }
				if (reservation2.Target == target && reservation2.Claimant == claimant && reservation2.Job == job)
				{
					reservation1 = reservation2;
					break;
				}
			}
			if (reservation1 == null && !target.ThingDestroyed)
				Log.Error("Tried to release " + target + " that wasn't reserved by " + claimant + ".", false);
			else
				lock (reservations(__instance)) {
					reservations(__instance).Remove(reservation1);
				}
			return false;
		}
		
		private static bool RespectsReservationsOf(Pawn newClaimant, Pawn oldClaimant)
		{
			return newClaimant == oldClaimant || newClaimant.Faction != null && oldClaimant.Faction != null && (newClaimant.Faction == oldClaimant.Faction || !newClaimant.Faction.HostileTo(oldClaimant.Faction) || oldClaimant.HostFaction != null && oldClaimant.HostFaction == newClaimant.HostFaction || newClaimant.HostFaction != null && (oldClaimant.HostFaction != null || newClaimant.HostFaction == oldClaimant.Faction));
		}
		public static bool FirstRespectedReserver(ReservationManager __instance, ref Pawn __result, LocalTargetInfo target, Pawn claimant)
		{
			if (!target.IsValid)
			{
				__result = null;
				return false;
			}
			Reservation reservation;
			for (int i = 0; i < reservations(__instance).Count; i++)
			{
				try
				{
					reservation = reservations(__instance)[i];
				} catch (ArgumentOutOfRangeException) { break; }
				if(null == reservation)
                {
					continue;
                }
				if (reservation.Target == target && RespectsReservationsOf(claimant, reservation.Claimant))
				{
					__result = reservation.Claimant;
					return false;
				}
			}
			__result = null;
			return false;
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
			{
				Log.ErrorOnce("Reserving with maxPawns > 1 and stackCount = All; this will not have a useful effect (suppressing future warnings)", 83269, false);
			}

			if (job == null)
			{
				Log.Warning(claimant.ToStringSafe() + " tried to reserve thing " + target.ToStringSafe() + " without a valid job");
				__result = false;
				return false;
			}
			int num1 = (!target.HasThing) ? 1 : target.Thing.stackCount;
			int num2 = stackCount == -1 ? num1 : stackCount;
			lock (reservations(__instance))
			{
				for (int index = 0; index < reservations(__instance).Count; ++index)
				{
					Reservation reservation1;
					try
					{
						reservation1 = reservations(__instance)[index];
					} catch (ArgumentOutOfRangeException) { break; }
					if (reservation1 != null && reservation1.Target == target && reservation1.Claimant == claimant && reservation1.Job == job && reservation1.Layer == layer && (reservation1.StackCount == -1 || reservation1.StackCount >= num2))
					{
						__result = true;
						return false;
					}
				}
				if (!target.IsValid || target.ThingDestroyed)
				{
					__result = false;
					return false;
				}
				bool canReserveResult = __instance.CanReserve(claimant, target, maxPawns, stackCount, layer);
				if (!canReserveResult)
				{
					//bool canReserveResult2 = __instance.CanReserve(claimant, target, maxPawns, stackCount, layer);
					if (job != null && job.playerForced && __instance.CanReserve(claimant, target, maxPawns, stackCount, layer))
					{
						reservations(__instance).Add(new Reservation(claimant, job, maxPawns, stackCount, target, layer));					
						//foreach (ReservationManager.Reservation reservation in reservations(__instance).ToList<ReservationManager.Reservation>())
						Reservation reservation2;
						for (int index = 0; index < reservations(__instance).Count; index++)
						{
							try
							{
								reservation2 = reservations(__instance)[index];
							}
							catch (ArgumentOutOfRangeException) { break; }
							if (reservation2.Target == target && reservation2.Claimant != claimant && (reservation2.Layer == layer && RespectsReservationsOf(claimant, reservation2.Claimant)))
								reservation2.Claimant.jobs.EndCurrentOrQueuedJob(reservation2.Job, JobCondition.InterruptForced);
						}
						__result = true;
						return false;
					}
				
					//HACK - Probably because Reserve is no longer valid after CanReserve time delay with multiple threads.
					if (errorOnFailed)
					{
						//LogCouldNotReserveError(__instance, claimant, job, target, maxPawns, stackCount, layer);
						Log.Warning("ReservationManager.Reserve cannot reserve. This is likely because reservation is no longer valid after CanReserve was called due to time delay with multiple threads.");
					}
					__result = false;
					return false;
				}
				reservations(__instance).Add(new ReservationManager.Reservation(claimant, job, maxPawns, stackCount, target, layer));
			}
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
		public static bool CanReserveStack(ReservationManager __instance, ref int __result, Pawn claimant, LocalTargetInfo target, int maxPawns = 1, ReservationLayerDef layer = null, bool ignoreOtherReservations = false)
		{
			if (claimant == null)
			{
				Log.Error("CanReserve with null claimant");
				__result = 0;
				return false;
			}

			if (!claimant.Spawned || claimant.Map != map(__instance))
			{
				__result = 0;
				return false;
			}

			if (!target.IsValid || target.ThingDestroyed)
			{
				__result = 0;
				return false;
			}

			if (target.HasThing && target.Thing.SpawnedOrAnyParentSpawned && target.Thing.MapHeld != map(__instance))
			{
				__result = 0;
				return false;
			}

			int num = (!target.HasThing) ? 1 : target.Thing.stackCount;
			int num2 = 0;
			if (!ignoreOtherReservations)
			{
				if (map(__instance).physicalInteractionReservationManager.IsReserved(target) && !map(__instance).physicalInteractionReservationManager.IsReservedBy(claimant, target))
				{
					__result = 0;
					return false;
				}

				int num3 = 0;
				Reservation reservation;
				for (int i = 0; i < reservations(__instance).Count; i++)
				{
					try
					{
						reservation = reservations(__instance)[i];
					} catch(ArgumentOutOfRangeException) { break; }
					if (null != reservation)
					{
						if (!(reservation.Target != target) && reservation.Layer == layer && reservation.Claimant != claimant && RespectsReservationsOf(claimant, reservation.Claimant))
						{
							if (reservation.MaxPawns != maxPawns)
							{
								__result = 0;
								return false;
							}

							num3++;
							num2 = ((reservation.StackCount != -1) ? (num2 + reservation.StackCount) : (num2 + num));
							if (num3 >= maxPawns || num2 >= num)
							{
								__result = 0;
								return false;
							}
						}
					}
				}
			}

			__result = Mathf.Max(num - num2, 0);
			return false;
		}


		public static bool FirstReservationFor(ReservationManager __instance, ref LocalTargetInfo __result, Pawn claimant)
		{
			if (claimant == null)
			{
				__result = LocalTargetInfo.Invalid;
				return false;
			}
			//ReservationManager.Reservation[] reservations2 = reservations(__instance).ToArray();
			ReservationManager.Reservation r;
			for (int i = 0; i < reservations(__instance).Count; i++)
			{
				try
				{
					r = reservations(__instance)[i];
				} catch (ArgumentOutOfRangeException) { break; }
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
		public static bool CanReserve(ReservationManager __instance, ref bool __result, Pawn claimant, LocalTargetInfo target, int maxPawns = 1, int stackCount = -1, ReservationLayerDef layer = null, bool ignoreOtherReservations = false)
		{
			if (claimant == null)
			{
				Log.Error("CanReserve with null claimant");
				return false;
			}

			if (!claimant.Spawned || claimant.Map != map(__instance))
			{
				return false;
			}

			if (!target.IsValid || target.ThingDestroyed)
			{
				return false;
			}

			if (target.HasThing && target.Thing.SpawnedOrAnyParentSpawned && target.Thing.MapHeld != map(__instance))
			{
				return false;
			}

			int num = (!target.HasThing) ? 1 : target.Thing.stackCount;
			int num2 = (stackCount == -1) ? num : stackCount;
			if (num2 > num)
			{
				return false;
			}

			if (!ignoreOtherReservations)
			{
				if (map(__instance).physicalInteractionReservationManager.IsReserved(target) && !map(__instance).physicalInteractionReservationManager.IsReservedBy(claimant, target))
				{
					return false;
				}

				for (int i = 0; i < reservations(__instance).Count; i++)
				{
					Reservation reservation;
					try
					{
						reservation = reservations(__instance)[i];
					} catch(ArgumentOutOfRangeException)
                    {
						break;
                    }
					if (reservation != null && reservation.Target == target && 
						reservation.Layer == layer && reservation.Claimant == claimant && 
						(reservation.StackCount == -1 || reservation.StackCount >= num2))
					{
						return true;
					}
				}

				int num3 = 0;
				int num4 = 0;
				for (int j = 0; j < reservations(__instance).Count; j++)
				{
					Reservation reservation2;
					try
                    {
						reservation2 = reservations(__instance)[j];
					} catch(ArgumentOutOfRangeException)
                    {
						break;
                    }
					if (reservation2 != null && !(reservation2.Target != target) && reservation2.Layer == layer && reservation2.Claimant != claimant && RespectsReservationsOf(claimant, reservation2.Claimant))
					{
						if (reservation2.MaxPawns != maxPawns)
						{
							return false;
						}

						num3++;
						num4 = ((reservation2.StackCount != -1) ? (num4 + reservation2.StackCount) : (num4 + num));
						if (num3 >= maxPawns || num2 + num4 > num)
						{
							return false;
						}
					}
				}
			}

			return true;
		}



	}
}

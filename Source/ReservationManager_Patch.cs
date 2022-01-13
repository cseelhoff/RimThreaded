using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using static Verse.AI.ReservationManager;
using UnityEngine;
using System.Text;
using System;
using System.Threading;

namespace RimThreaded
{
	[StaticConstructorOnStartup]
	public class ReservationManager_Patch
	{
		public static readonly Dictionary<ReservationManager, Dictionary<LocalTargetInfo, List<Reservation>>> reservationTargetDicts =
			new Dictionary<ReservationManager, Dictionary<LocalTargetInfo, List<Reservation>>>();
		public static readonly Dictionary<ReservationManager, Dictionary<Pawn, List<Reservation>>> reservationClaimantDicts =
			new Dictionary<ReservationManager, Dictionary<Pawn, List<Reservation>>>();

		[ThreadStatic] static public List<Reservation> newReservationClaimantList;
		[ThreadStatic] static public List<Reservation> newReservationTargetList;
		
		private static ReaderWriterLockSlim rwl = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);

		internal static void RunDestructivePatches()
		{
			Type original = typeof(ReservationManager);
			Type patched = typeof(ReservationManager_Patch);
			RimThreadedHarmony.Prefix(original, patched, nameof(CanReserve));
			RimThreadedHarmony.Prefix(original, patched, nameof(CanReserveStack));
			RimThreadedHarmony.Prefix(original, patched, nameof(Reserve));
			RimThreadedHarmony.Prefix(original, patched, nameof(Release));
			RimThreadedHarmony.Prefix(original, patched, nameof(ReleaseAllForTarget));
			RimThreadedHarmony.Prefix(original, patched, nameof(ReleaseClaimedBy));
			RimThreadedHarmony.Prefix(original, patched, nameof(ReleaseAllClaimedBy));
			RimThreadedHarmony.Prefix(original, patched, nameof(FirstReservationFor));
			RimThreadedHarmony.Prefix(original, patched, nameof(IsReservedByAnyoneOf));
			RimThreadedHarmony.Prefix(original, patched, nameof(FirstRespectedReserver));
			RimThreadedHarmony.Prefix(original, patched, nameof(ReservedBy), new Type[] { typeof(LocalTargetInfo), typeof(Pawn), typeof(Job) });
			//RimThreadedHarmony.Prefix(original, patched, "ReservedByJobDriver_TakeToBed"); //TODO FIX!
			RimThreadedHarmony.Prefix(original, patched, nameof(AllReservedThings));
			RimThreadedHarmony.Prefix(original, patched, nameof(DebugString));
			RimThreadedHarmony.Prefix(original, patched, nameof(DebugDrawReservations));
			RimThreadedHarmony.Prefix(original, patched, nameof(ExposeData));

			//RimThreadedHarmony.Postfix(original, patched, "Release", "PostRelease");
			//RimThreadedHarmony.Postfix(original, patched, "Release", "PostReleaseAllForTarget");
		}
		internal static void InitializeThreadStatics()
		{
			newReservationClaimantList = new List<Reservation>();
			newReservationTargetList = new List<Reservation>();
		}

		public static bool ExposeData(ReservationManager __instance)
		{
			__instance.reservations.Clear();
			foreach(Reservation reservation1 in getAllReservations(__instance))
            {
				__instance.reservations.Add(reservation1);
			}

			Scribe_Collections.Look(ref __instance.reservations, "reservations", LookMode.Deep);
			if (Scribe.mode != LoadSaveMode.PostLoadInit)
			{
				return false;
			}
			for (int num = __instance.reservations.Count - 1; num >= 0; num--)
			{
				Reservation reservation = __instance.reservations[num];
				if (reservation.Target.Thing != null && reservation.Target.Thing.Destroyed)
				{
					Log.Error(string.Concat("Loaded reservation with destroyed target: ", reservation, ". Deleting it..."));
					__instance.reservations.Remove(reservation);
				}
				if (reservation.Claimant != null && reservation.Claimant.Destroyed)
				{
					Log.Error(string.Concat("Loaded reservation with destroyed claimant: ", reservation, ". Deleting it..."));
					__instance.reservations.Remove(reservation);
				}
				if (reservation.Claimant == null)
				{
					Log.Error(string.Concat("Loaded reservation with null claimant: ", reservation, ". Deleting it..."));
					__instance.reservations.Remove(reservation);
				}
				if (reservation.Job == null)
				{
					Log.Error(string.Concat("Loaded reservation with null job: ", reservation, ". Deleting it..."));
					__instance.reservations.Remove(reservation);
				}
			}
			return false;
		}

		private static List<Reservation> getReservationTargetList(Dictionary<LocalTargetInfo, List<Reservation>> reservationTargetDict, LocalTargetInfo target)
		{
			if (!reservationTargetDict.TryGetValue(target, out List<Reservation> reservationTargetList))
			{
				lock (reservationTargetDict)
				{
					if (!reservationTargetDict.TryGetValue(target, out List<Reservation> reservationTargetList2))
					{
						reservationTargetList = new List<Reservation>();
						reservationTargetDict[target] = reservationTargetList;
					}
					else
					{
						reservationTargetList = reservationTargetList2;
					}
				}
			}
			return reservationTargetList;
		}
		private static List<Reservation> getReservationClaimantList(Dictionary<Pawn, List<Reservation>> reservationClaimantDict, Pawn claimant)
		{
			if (!reservationClaimantDict.TryGetValue(claimant, out List<Reservation> reservationClaimantList))
			{
				lock (reservationClaimantDict)
				{
					if (!reservationClaimantDict.TryGetValue(claimant, out List<Reservation> reservationClaimantList2))
					{
						reservationClaimantList = new List<Reservation>();
						reservationClaimantDict[claimant] = reservationClaimantList;
					}
					else
					{
						reservationClaimantList = reservationClaimantList2;
					}
				}
			}
			return reservationClaimantList;
		}
		public static List<Reservation> getReservationTargetList(ReservationManager __instance, LocalTargetInfo target)
		{
			Dictionary<LocalTargetInfo, List<Reservation>> reservationTargetDict = getReservationTargetDict(__instance);
			if (!reservationTargetDict.TryGetValue(target, out List<Reservation> reservationTargetList))
			{
				lock (reservationTargetDict)
				{
					if (!reservationTargetDict.TryGetValue(target, out List<Reservation> reservationTargetList2))
					{
						reservationTargetList = new List<Reservation>();
						reservationTargetDict[target] = reservationTargetList;
					}
					else
					{
						reservationTargetList = reservationTargetList2;
					}
				}
			}
			return reservationTargetList;
		}
		private static List<Reservation> getReservationClaimantList(ReservationManager __instance, Pawn claimant)
		{
			Dictionary<Pawn, List<Reservation>> reservationClaimantDict = getReservationClaimantDict(__instance);
			if (!reservationClaimantDict.TryGetValue(claimant, out List<Reservation> reservationClaimantList))
			{
				lock (reservationClaimantDict)
				{
					if (!reservationClaimantDict.TryGetValue(claimant, out List<Reservation> reservationClaimantList2))
					{
						reservationClaimantList = new List<Reservation>();
						reservationClaimantDict[claimant] = reservationClaimantList;
					}
					else
					{
						reservationClaimantList = reservationClaimantList2;
					}
				}
			}
			return reservationClaimantList;
		}
		private static Dictionary<LocalTargetInfo, List<Reservation>> getReservationTargetDict(ReservationManager __instance)
		{
			if (!reservationTargetDicts.TryGetValue(__instance, out Dictionary<LocalTargetInfo, List<Reservation>> reservationTargetDict))
			{
				 rwl.EnterWriteLock();
				try
				{
					if (!reservationTargetDicts.TryGetValue(__instance, out Dictionary<LocalTargetInfo, List<Reservation>> reservationTargetDict2))
					{
						Log.Message("RimThreaded is building new Reservations Dictionary...");
						reservationTargetDict = new Dictionary<LocalTargetInfo, List<Reservation>>();
						Dictionary<Pawn, List<Reservation>> reservationClaimantDict = new Dictionary<Pawn, List<Reservation>>();
						foreach (Reservation reservation in __instance.reservations)
						{
							LocalTargetInfo localTargetInfo = reservation.Target;
							if (!reservationTargetDict.TryGetValue(localTargetInfo, out List<Reservation> reservationTargetList))
							{
								reservationTargetList = new List<Reservation>();
								reservationTargetDict[localTargetInfo] = reservationTargetList;
							}
							reservationTargetList.Add(reservation);

							Pawn claimant = reservation.Claimant;
							if (!reservationClaimantDict.TryGetValue(claimant, out List<Reservation> reservationClaimantList))
							{
								reservationClaimantList = new List<Reservation>();
								reservationClaimantDict[claimant] = reservationClaimantList;
							}
							reservationClaimantList.Add(reservation);
						}
						reservationTargetDicts[__instance] = reservationTargetDict;
						reservationClaimantDicts[__instance] = reservationClaimantDict;
					}
					else
					{
						reservationTargetDict = reservationTargetDict2;
					}
				}
				finally
				{
					rwl.ExitWriteLock();
				}
			}
			return reservationTargetDict;
		}
		private static Dictionary<Pawn, List<Reservation>> getReservationClaimantDict(ReservationManager __instance)
		{
            if (reservationClaimantDicts.TryGetValue(__instance, out Dictionary<Pawn, List<Reservation>> reservationClaimantDict)) 
                return reservationClaimantDict;
            rwl.EnterWriteLock();
			try
			{
                if (!reservationClaimantDicts.TryGetValue(__instance, out Dictionary<Pawn, List<Reservation>> reservationClaimantDict2))
                {
                    Log.Message("RimThreaded is building new Reservations Dictionary...");
                    Dictionary<LocalTargetInfo, List<Reservation>> reservationTargetDict = new Dictionary<LocalTargetInfo, List<Reservation>>();
                    reservationClaimantDict = new Dictionary<Pawn, List<Reservation>>();
                    foreach (Reservation reservation in __instance.reservations)
                    {
                        LocalTargetInfo localTargetInfo = reservation.Target;
                        if (!reservationTargetDict.TryGetValue(localTargetInfo, out List<Reservation> reservationTargetList))
                        {
                            reservationTargetList = new List<Reservation>();
                            reservationTargetDict[localTargetInfo] = reservationTargetList;
                        }
                        reservationTargetList.Add(reservation);

                        Pawn claimant = reservation.Claimant;
                        if (!reservationClaimantDict.TryGetValue(claimant, out List<Reservation> reservationClaimantList))
                        {
                            reservationClaimantList = new List<Reservation>();
                            reservationClaimantDict[claimant] = reservationClaimantList;
                        }
                        reservationClaimantList.Add(reservation);
                    }
                    reservationTargetDicts[__instance] = reservationTargetDict;
                    reservationClaimantDicts[__instance] = reservationClaimantDict;
                }
                else
                {
                    reservationClaimantDict = reservationClaimantDict2;
                }
            }
			finally
			{
				rwl.ExitWriteLock();
			}
            return reservationClaimantDict;
		}

		public static Pawn getFirstPawnReservingTarget(ReservationManager __instance, LocalTargetInfo target)
        {
			List<Reservation> reservationTargetListUnsafe = getReservationTargetList(__instance, target);
			foreach(Reservation r in reservationTargetListUnsafe)
            {
				return r.Claimant;
            }
			return null;
		}

		public static void PostRelease(ReservationManager __instance, LocalTargetInfo target, Pawn claimant, Job job)
		{
			if (target.Thing != null && target.Thing.def.EverHaulable && target.Thing.Map != null)
			{
				HaulingCache.ReregisterHaulableItem(target.Thing);
			}
		}
		public static void PostReleaseAllForTarget(ReservationManager __instance, LocalTargetInfo target, Pawn claimant, Job job)
		{
			if (target.Thing != null && target.Thing.def.EverHaulable && target.Thing.Map != null)
			{
				HaulingCache.ReregisterHaulableItem(target.Thing);
			}
		}

		public static bool CanReserve(ReservationManager __instance, ref bool __result, Pawn claimant, LocalTargetInfo target, int maxPawns = 1, int stackCount = -1, ReservationLayerDef layer = null, bool ignoreOtherReservations = false)
		{
			Map mapInstance = __instance.map;

			if (claimant == null)
			{
				Log.Error("CanReserve with null claimant");
				__result = false;
				return false;
			}

			if (!claimant.Spawned || claimant.Map != mapInstance)
			{
				__result = false;
				return false;
			}

			if (!target.IsValid || target.ThingDestroyed)
			{
				__result = false;
				return false;
			}

			if (target.HasThing && target.Thing.SpawnedOrAnyParentSpawned && target.Thing.MapHeld != mapInstance)
			{
				__result = false;
				return false;
			}

			int num = (!target.HasThing) ? 1 : target.Thing.stackCount;
			int num2 = (stackCount == -1) ? num : stackCount;
			if (num2 > num)
			{
				__result = false;
				return false;
			}

			if (!ignoreOtherReservations)
			{
				if (mapInstance.physicalInteractionReservationManager.IsReserved(target) && !mapInstance.physicalInteractionReservationManager.IsReservedBy(claimant, target))
				{
					__result = false;
					return false;
				}
				int num3 = 0;
				int num4 = 0;
				List<Reservation> reservationTargetList = getReservationTargetList(__instance, target);
				rwl.EnterReadLock();
				try
				{
					foreach (Reservation reservation in reservationTargetList)
					{
						if (reservation.Layer == layer)
						{
							if (reservation.Claimant == claimant && (reservation.StackCount == -1 || reservation.StackCount >= num2))
							{
								__result = true;
								return false;
							}
							if (reservation.Claimant != claimant && RespectsReservationsOf(claimant, reservation.Claimant))
							{
								if (reservation.MaxPawns != maxPawns)
								{
									__result = false;
									return false;
								}

								num3++;
								num4 = (reservation.StackCount != -1) ? (num4 + reservation.StackCount) : (num4 + num);
								if (num3 >= maxPawns || num2 + num4 > num)
								{
									__result = false;
									return false;
								}
							}
						}
					}
				}
				finally
				{
					rwl.ExitReadLock();
				}
			}

			return true;
		}

		public static bool IsUnreserved(ReservationManager __instance, LocalTargetInfo target, int maxPawns = 1, int stackCount = -1, ReservationLayerDef layer = null, bool ignoreOtherReservations = false)
		{
			Map mapInstance = __instance.map;

			if (!target.IsValid || target.ThingDestroyed)
			{
				return false;
			}

			if (target.HasThing && target.Thing.SpawnedOrAnyParentSpawned && target.Thing.MapHeld != mapInstance)
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
				if (mapInstance.physicalInteractionReservationManager.IsReserved(target))
				{
					return false;
				}
				int num3 = 0;
				int num4 = 0;
				List<Reservation> reservationTargetList = getReservationTargetList(__instance, target);
				foreach (Reservation reservation in reservationTargetList)
				{
					if (reservation.Layer == layer)
					{
						if (reservation.MaxPawns != maxPawns)
						{
							return false;
						}
						num3++;
						num4 = (reservation.StackCount != -1) ? (num4 + reservation.StackCount) : (num4 + num);
						if (num3 >= maxPawns || num2 + num4 > num)
						{
							return false;
						}
					}
				}
			}

			return true;
		}

		public static bool CanReserveStack(ReservationManager __instance, ref int __result, Pawn claimant, LocalTargetInfo target, int maxPawns = 1, ReservationLayerDef layer = null, bool ignoreOtherReservations = false)
		{
			Map mapInstance = __instance.map;

			if (claimant == null)
			{
				Log.Error("CanReserve with null claimant");
				__result = 0;
				return false;
			}

			if (!claimant.Spawned || claimant.Map != mapInstance)
			{
				__result = 0;
				return false;
			}

			if (!target.IsValid || target.ThingDestroyed)
			{
				__result = 0;
				return false;
			}

			if (target.HasThing && target.Thing.SpawnedOrAnyParentSpawned && target.Thing.MapHeld != mapInstance)
			{
				__result = 0;
				return false;
			}

			int num = (!target.HasThing) ? 1 : target.Thing.stackCount;
			int num2 = 0;
			if (!ignoreOtherReservations)
			{
				if (mapInstance.physicalInteractionReservationManager.IsReserved(target) && !mapInstance.physicalInteractionReservationManager.IsReservedBy(claimant, target))
				{
					__result = 0;
					return false;
				}

				int num3 = 0;
				List<Reservation> reservationTargetList = getReservationTargetList(__instance, target);
				foreach (Reservation reservation in reservationTargetList)
				{
					if (reservation.Layer == layer && reservation.Claimant != claimant && RespectsReservationsOf(claimant, reservation.Claimant))
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

			__result = Mathf.Max(num - num2, 0);
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
				Log.ErrorOnce("Reserving with maxPawns > 1 and stackCount = All; this will not have a useful effect (suppressing future warnings)", 83269);
			}

			if (job == null)
			{
				Log.Warning(claimant.ToStringSafe() + " tried to reserve thing " + target.ToStringSafe() + " without a valid job");
				__result = false;
				return false;
			}
			Thing thing = target.Thing;
			int num1 = (!target.HasThing) ? 1 : thing.stackCount;
			int num2 = stackCount == -1 ? num1 : stackCount;

			List<Reservation> reservationTargetList = getReservationTargetList(__instance, target);
			rwl.EnterUpgradeableReadLock();
			try
			{
				foreach (Reservation reservation1 in reservationTargetList)
				{
                    if (reservation1.Claimant == claimant && reservation1.Job == job && reservation1.Layer == layer)
                    {
                        if (reservation1.StackCount == -1 || reservation1.StackCount >= num2)
                        {
                            __result = true;
                            return false;
                        }
						Log.Warning("Debug reservation");
                    }
                }
				if (!target.IsValid || target.ThingDestroyed) //TODO reservation should be removed when thing is destroyed
				{
					__result = false;
					return false;
				}
				bool canReserveResult = __instance.CanReserve(claimant, target, maxPawns, stackCount, layer);
				Reservation reservation;
				if (!canReserveResult)
				{
					if (job.playerForced && __instance.CanReserve(claimant, target, maxPawns, stackCount, layer, true))
					{
						//reservation = new Reservation(claimant, job, maxPawns, stackCount, target, layer);
						reservation = SimplePool_Patch<Reservation>.Get();
						reservation.claimant = claimant;
						reservation.job = job;
						reservation.maxPawns = maxPawns;
						reservation.stackCount = stackCount;
						reservation.target = target;
						reservation.layer = layer;

						List<Reservation> claimantReservations= getReservationClaimantList(__instance, claimant);
						getReservationTargetList(__instance, target).Add(reservation);
						claimantReservations.Add(reservation);
						

                        foreach (Reservation reservation2 in claimantReservations)
						{
							if (reservation2.Claimant != claimant && (reservation2.Layer == layer && RespectsReservationsOf(claimant, reservation2.Claimant)))
								reservation2.Claimant.jobs.EndCurrentOrQueuedJob(reservation2.Job, JobCondition.InterruptForced);
						}
						if (thing != null && thing.def.EverHaulable)
						{
							//Log.Message("DeregisterHaulableItem " + haulableThing.ToString());
							HaulingCache.DeregisterHaulableItem(thing);
						}
						if (thing is Plant plant)
						{
							JumboCell.ReregisterObject(__instance.map, plant.Position, RimThreaded.plantHarvest_Cache);
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
				//reservation = new Reservation(claimant, job, maxPawns, stackCount, target, layer);
				reservation = SimplePool_Patch<Reservation>.Get();
				reservation.claimant = claimant;
				reservation.job = job;
				reservation.maxPawns = maxPawns;
				reservation.stackCount = stackCount;
				reservation.target = target;
				reservation.layer = layer;

                getReservationTargetList(__instance, target).Add(reservation);
				getReservationClaimantList(__instance, claimant).Add(reservation);
			}
			finally
			{
				rwl.ExitUpgradeableReadLock();
			}
			__result = true;
			return false;
		}

		public static bool Release(ReservationManager __instance, LocalTargetInfo target, Pawn claimant, Job job)
		{
			if (target.ThingDestroyed)
			{
				Log.Warning("Releasing destroyed thing " + target + " for " + claimant);
			}

			List<Reservation> tl = getReservationTargetList(__instance, target);
			List<Reservation> cl = getReservationClaimantList(__instance, claimant);
			rwl.EnterWriteLock();
			try
			{
				int trlIdx = tl.FindIndex(r => r.Claimant == claimant && r.Job == job);
				if (trlIdx==-1)
					Log.Warning("Tried to release " + target + " that wasn't reserved by " + claimant + ".");
				else
				{
					cl.RemoveAll(r => r.Claimant == claimant && r.Job == job);
					SimplePool_Patch<Reservation>.Return(tl[trlIdx]);
					tl.RemoveAt(trlIdx);
				}
			}
			finally
			{
				rwl.ExitWriteLock();
			}

			//Postfix
			Thing thing = target.Thing;
            if (thing != null && thing.def.EverHaulable)
            {
                HaulingCache.ReregisterHaulableItem(target.Thing);
            }
			if (thing is Plant plant)
			{
				JumboCell.ReregisterObject(__instance.map, plant.Position, RimThreaded.plantHarvest_Cache);
			}

			return false;
		}
		public static bool ReleaseAllForTarget(ReservationManager __instance, Thing t)
		{
			if (t == null)
			{
				return false;
			}
			List<Reservation> rtl = getReservationTargetList(__instance, t.Position);
			rwl.EnterWriteLock();
			try
			{
				foreach (Reservation reservation in rtl)
				{
					Thing thing = reservation.Target.Thing;
					if (thing == t)
					{
						//remove
						getReservationClaimantList(__instance, reservation.claimant).RemoveAll(r => r.Target.Thing == t);
						SimplePool_Patch<Reservation>.Return(reservation);

					}
				}

				rtl.RemoveAll(r => r.Target.Thing == t);
			}
			finally
			{
				rwl.ExitWriteLock();
			}

			//HaulingCache
			if (t.def.EverHaulable)
			{
				HaulingCache.ReregisterHaulableItem(t);
			}
			if (t is Plant plant)
			{
				JumboCell.ReregisterObject(__instance.map, plant.Position, RimThreaded.plantHarvest_Cache);
			}
			return false;
		}
		public static bool ReleaseClaimedBy(ReservationManager __instance, Pawn claimant, Job job)
		{
			List<Reservation> reservationClaimantList = getReservationClaimantList(__instance, claimant);
			rwl.EnterWriteLock();
			//list of things to update caches for. I do it this way so that I can release the lock before updating the caches.
			List<Thing> thingList = new List<Thing>();
			try
			{
				foreach (Reservation reservation in reservationClaimantList)
				{
					if (reservation.Job == job)
					{
						LocalTargetInfo target = reservation.Target;
						getReservationTargetList(__instance, target).RemoveAll(r => r.Claimant == claimant && r.Job == job);
						thingList.Add(target.Thing);
						SimplePool_Patch<Reservation>.Return(reservation);

					}
				}
				reservationClaimantList.RemoveAll(r => r.Job == job);

			}
			finally
			{
				rwl.ExitWriteLock();
			}

			foreach (Thing t in thingList)
			{

				if (t != null && t.def.EverHaulable)
				{
					HaulingCache.ReregisterHaulableItem(t);
				}
				if (t is Plant plant)
				{
					JumboCell.ReregisterObject(__instance.map, plant.Position, RimThreaded.plantHarvest_Cache);
				}
			}
			return false;
		}

		public static bool ReleaseAllClaimedBy(ReservationManager __instance, Pawn claimant)
		{
			List<Reservation> reservationClaimantList = getReservationClaimantList(__instance, claimant);
			//list of things to update caches for. I do it this way so that I can release the lock before updating the caches.
			List<Thing> thingList = new List<Thing>();
			rwl.EnterWriteLock();
			try
			{
				foreach (Reservation reservation in reservationClaimantList)
				{
					LocalTargetInfo target = reservation.Target;

					getReservationTargetList(__instance, target).RemoveAll(r => r.claimant == claimant);
					thingList.Add(target.Thing);
					SimplePool_Patch<Reservation>.Return(reservation);
				}
				reservationClaimantList.Clear();
			}
			finally
			{
				rwl.ExitWriteLock();
			}


			//HaulingCache
			foreach (Thing t in thingList)
			{
				if (t != null && t.def.EverHaulable)
				{
					HaulingCache.ReregisterHaulableItem(t);
				}
				if (t is Plant plant)
				{
					JumboCell.ReregisterObject(__instance.map, plant.Position, RimThreaded.plantHarvest_Cache);
				}
			}
			return false;
		}
		public static bool FirstReservationFor(ReservationManager __instance, ref LocalTargetInfo __result, Pawn claimant)
		{
			if (claimant == null)
			{
				__result = LocalTargetInfo.Invalid;
				return false;
			}
			List<Reservation> reservationClaimantList = getReservationClaimantList(__instance, claimant);			
			rwl.EnterReadLock();
			try
			{
				foreach (Reservation reservation in reservationClaimantList)
				{
					__result = reservation.Target;
					return false;
				}			
			}
			finally
			{
				rwl.ExitReadLock();
			}
			__result = LocalTargetInfo.Invalid;
			return false;
		}
		public static bool IsReservedByAnyoneOf(ReservationManager __instance, ref bool __result, LocalTargetInfo target, Faction faction)
		{
			if (!target.IsValid)
			{
				__result = false;
				return false;
			}

			List<Reservation> reservationTargetList = getReservationTargetList(__instance, target);
			rwl.EnterReadLock();
			try
			{
				foreach(Reservation reservation in reservationTargetList)
				{
					if (reservation.Claimant.Faction == faction)
					{
						__result = true;
						return false;
					}
				}
			}
			finally
			{
				rwl.ExitReadLock();
			}
			__result = false;
			return false;
		}

		public static bool RespectsReservationsOf(Pawn newClaimant, Pawn oldClaimant)
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

			List<Reservation> reservationTargetList = getReservationTargetList(__instance, target);
			rwl.EnterReadLock();
			try
			{
				foreach (Reservation reservation in reservationTargetList)
				{
					if (RespectsReservationsOf(claimant, reservation.Claimant))
					{
						__result = reservation.Claimant;
						return false;
					}
				}
			}
			finally
			{
				rwl.ExitReadLock();
			}
			__result = null;
			return false;
		}
		public static bool ReservedBy(ReservationManager __instance, ref bool __result, LocalTargetInfo target, Pawn claimant, Job job = null)
		{
			if (!target.IsValid)
			{
				__result = false;
				return false;
			}

			List<Reservation> reservationTargetList = getReservationTargetList(__instance, target);
			rwl.EnterReadLock();
			try
			{
				foreach (Reservation reservation in reservationTargetList)
				{
					if (reservation.Claimant == claimant && (job == null || reservation.Job == job))
					{
						__result = true;
						return false;
					}
				}
			}
			finally
			{
				rwl.ExitReadLock();
			}
			__result = false;
			return false;
		}
		public static bool ReservedByJobDriver_TakeToBed(ReservationManager __instance, ref bool __result, LocalTargetInfo target, Pawn claimant, LocalTargetInfo? targetAIsNot = null, LocalTargetInfo? targetBIsNot = null, LocalTargetInfo? targetCIsNot = null)
		{
			if (!target.IsValid)
			{
				__result = false;
			}

			List<Reservation> reservationTargetList = getReservationTargetList(__instance, target);
			rwl.EnterReadLock();
			try
			{
				foreach (Reservation reservation in reservationTargetList)
				{
					if (reservation.Claimant != claimant || reservation.Job == null || !(reservation.Job.GetCachedDriver(claimant) is JobDriver_TakeToBed))
					{
						continue;
					}
					if (targetAIsNot.HasValue)
					{
						LocalTargetInfo targetA = reservation.Job.targetA;
						LocalTargetInfo? localTargetInfo = targetAIsNot;
						if (!(targetA != localTargetInfo))
						{
							continue;
						}
					}
					if (targetBIsNot.HasValue)
					{
						LocalTargetInfo targetA = reservation.Job.targetB;
						LocalTargetInfo? localTargetInfo = targetBIsNot;
						if (!(targetA != localTargetInfo))
						{
							continue;
						}
					}
					if (targetCIsNot.HasValue)
					{
						LocalTargetInfo targetA = reservation.Job.targetC;
						LocalTargetInfo? localTargetInfo = targetCIsNot;
						if (!(targetA != localTargetInfo))
						{
							continue;
						}
					}
					__result = true;
					return false;
				}
			}
			finally
			{
				rwl.ExitReadLock();
			}
			__result = false;
			return false;
		}
		public static bool AllReservedThings(ReservationManager __instance, ref IEnumerable<Thing> __result)
		{
			//return reservations.Select((Reservation res) => res.Target.Thing);
			__result = AllReservedThings2(__instance);
			return false;
		}

        private static IEnumerable<Thing> AllReservedThings2(ReservationManager __instance)
        {
			foreach(Reservation reservation in getAllReservations(__instance))
            {
				yield return reservation.Target.Thing;
            }
		}
		public static bool DebugString(ReservationManager __instance, ref string __result)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("All reservation in ReservationManager:");
			int i = 0;
			foreach (Reservation reservation in getAllReservations(__instance))
			{
				stringBuilder.AppendLine("[" + (i++).ToString() + "] " + reservation.ToString());
			}
			__result = stringBuilder.ToString();
			return false;
		}
		public static bool DebugDrawReservations(ReservationManager __instance)
		{
			foreach (Reservation reservation in getAllReservations(__instance))
			{
				if (reservation.Target.Thing != null)
				{
					if (reservation.Target.Thing.Spawned)
					{
						Thing thing = reservation.Target.Thing;
						Vector3 s = new Vector3(thing.RotatedSize.x, 1f, thing.RotatedSize.z);
						Matrix4x4 matrix = default;
						matrix.SetTRS(thing.DrawPos + Vector3.up * 0.1f, Quaternion.identity, s);
						Graphics.DrawMesh(MeshPool.plane10, matrix, DebugReservedThingIcon, 0);
						GenDraw.DrawLineBetween(reservation.Claimant.DrawPos, reservation.Target.Thing.DrawPos);
					}
					else
					{
						Graphics.DrawMesh(MeshPool.plane03, reservation.Claimant.DrawPos + Vector3.up + new Vector3(0.5f, 0f, 0.5f), Quaternion.identity, DebugReservedThingIcon, 0);
					}
				}
				else
				{
					Graphics.DrawMesh(MeshPool.plane10, reservation.Target.Cell.ToVector3ShiftedWithAltitude(AltitudeLayer.MetaOverlays), Quaternion.identity, DebugReservedThingIcon, 0);
					GenDraw.DrawLineBetween(reservation.Claimant.DrawPos, reservation.Target.Cell.ToVector3ShiftedWithAltitude(AltitudeLayer.MetaOverlays));
				}				
			}
			return false;
		}
		private static IEnumerable<Reservation> getAllReservations(ReservationManager __instance)
        {
			Dictionary<LocalTargetInfo, List<Reservation>> reservationTargetDict = getReservationTargetDict(__instance); //could be getReservationClaimantDict if preferred
			rwl.EnterReadLock();
			try
			{
				foreach (List<Reservation> reservations in reservationTargetDict.Values)
				{
					foreach (Reservation reservation in reservations)
					{
						yield return reservation;
					}
				}
			}
			finally
			{
				rwl.ExitReadLock();
			}
		}

    }
}

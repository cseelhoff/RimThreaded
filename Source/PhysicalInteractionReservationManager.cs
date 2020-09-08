using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Verse;
using Verse.AI;

namespace RimThreaded
{
    public class PhysicalInteractionReservationManager_Patch
    {
        public static ConcurrentDictionary<PhysicalInteractionReservationManager, ConcurrentDictionary<LocalTargetInfo, ConcurrentDictionary<Pawn, Job>>> instanceTargetToPawnToJob = new ConcurrentDictionary<PhysicalInteractionReservationManager, ConcurrentDictionary<LocalTargetInfo, ConcurrentDictionary<Pawn, Job>>>();


        public static bool IsReservedBy(PhysicalInteractionReservationManager __instance, ref bool __result, Pawn claimant, LocalTargetInfo target)
        {
            __result = false;
            ConcurrentDictionary<LocalTargetInfo, ConcurrentDictionary<Pawn, Job>> targetToPawnToJob = 
                instanceTargetToPawnToJob.GetOrAdd(__instance, new ConcurrentDictionary<LocalTargetInfo, ConcurrentDictionary<Pawn, Job>>());
            if (targetToPawnToJob.TryGetValue(target, out ConcurrentDictionary<Pawn, Job> pawnToJob))
                __result = pawnToJob.ContainsKey(claimant);

            return __result;            
        }

        public static bool Reserve(PhysicalInteractionReservationManager __instance, Pawn claimant, Job job, LocalTargetInfo target)
        {
            ConcurrentDictionary<LocalTargetInfo, ConcurrentDictionary<Pawn, Job>> targetToPawnToJob =
                instanceTargetToPawnToJob.GetOrAdd(__instance, new ConcurrentDictionary<LocalTargetInfo, ConcurrentDictionary<Pawn, Job>>());

            ConcurrentDictionary<Pawn, Job> pawnToJob = targetToPawnToJob.GetOrAdd(target, new ConcurrentDictionary<Pawn, Job>());

            if (!pawnToJob.TryAdd(claimant, job))
            {
                Log.Warning(claimant.ToString() + " tried to reserve job " + job.ToString() + " on target " + (object)target + ", but it's already reserved by him.", false);
            } 
            return false;
        }

        public static bool Release(PhysicalInteractionReservationManager __instance, Pawn claimant, Job job, LocalTargetInfo target)
        {
            ConcurrentDictionary<LocalTargetInfo, ConcurrentDictionary<Pawn, Job>> targetToPawnToJob =
                instanceTargetToPawnToJob.GetOrAdd(__instance, new ConcurrentDictionary<LocalTargetInfo, ConcurrentDictionary<Pawn, Job>>());

            if (targetToPawnToJob.TryGetValue(target, out ConcurrentDictionary<Pawn, Job> pawnToJob))
            {
                if (pawnToJob.TryGetValue(claimant, out Job outJob))
                {
                    if (outJob == job)
                    {
                        if (pawnToJob.TryRemove(claimant, out _))
                        {
                            /*
                            if (pawnToJob.Count == 0)
                            {
                                if (!targetToPawnToJob.TryRemove(target, out _))
                                {
                                    Log.Warning("Failed to release target " + (object)target + ", from targetToPawnToJob Dictionary.", false);
                                }
                            }
                            */
                        }
                        else
                        {
                            Log.Warning(claimant.ToString() + " tried to release reservation on target " + (object)target + ", but it failed.", false);
                        }
                    }
                    else
                    {
                        Log.Warning(claimant.ToString() + " tried to release reservation on target " + (object)target + ", but job was different.", false);
                    }
                }
                else
                {
                    Log.Warning(claimant.ToString() + " tried to release reservation on target " + (object)target + ", but claimant was not found.", false);
                }
            }
            else
            {
                Log.Warning(claimant.ToString() + " tried to release reservation on target " + (object)target + ", but target had no physical reservations.", false);
            }
            return false;
        }


        public static bool FirstReserverOf(PhysicalInteractionReservationManager __instance, ref Pawn __result, LocalTargetInfo target)
        {
            __result = null;
            ConcurrentDictionary<LocalTargetInfo, ConcurrentDictionary<Pawn, Job>> targetToPawnToJob =
                instanceTargetToPawnToJob.GetOrAdd(__instance, new ConcurrentDictionary<LocalTargetInfo, ConcurrentDictionary<Pawn, Job>>());

            if (targetToPawnToJob.TryGetValue(target, out ConcurrentDictionary<Pawn, Job> pawnToJob))
            {
                __result = pawnToJob.First().Key;
            }
            return false;
        }

        public static bool FirstReservationFor(PhysicalInteractionReservationManager __instance, ref LocalTargetInfo __result, Pawn claimant)
        {
            ConcurrentDictionary<LocalTargetInfo, ConcurrentDictionary<Pawn, Job>> targetToPawnToJob =
                instanceTargetToPawnToJob.GetOrAdd(__instance, new ConcurrentDictionary<LocalTargetInfo, ConcurrentDictionary<Pawn, Job>>());

            __result = LocalTargetInfo.Invalid;
            foreach (KeyValuePair<LocalTargetInfo, ConcurrentDictionary<Pawn, Job>> pair in targetToPawnToJob)
            {
                if(pair.Value.ContainsKey(claimant))
                {
                    __result = pair.Key;
                    break;
                }
            }
            return false;
        }

        public static bool ReleaseAllForTarget(PhysicalInteractionReservationManager __instance, LocalTargetInfo target)
        {
            ConcurrentDictionary<LocalTargetInfo, ConcurrentDictionary<Pawn, Job>> targetToPawnToJob =
                instanceTargetToPawnToJob.GetOrAdd(__instance, new ConcurrentDictionary<LocalTargetInfo, ConcurrentDictionary<Pawn, Job>>());

            targetToPawnToJob.TryRemove(target, out _);
            return false;
        }

        public static bool ReleaseClaimedBy(PhysicalInteractionReservationManager __instance, Pawn claimant, Job job)
        {
            ConcurrentDictionary<LocalTargetInfo, ConcurrentDictionary<Pawn, Job>> targetToPawnToJob =
                instanceTargetToPawnToJob.GetOrAdd(__instance, new ConcurrentDictionary<LocalTargetInfo, ConcurrentDictionary<Pawn, Job>>());

            foreach (LocalTargetInfo key in targetToPawnToJob.Keys.ToList())
            {
                if (targetToPawnToJob.TryGetValue(key, out ConcurrentDictionary<Pawn, Job> pawnToJob))
                {
                    if (pawnToJob.TryGetValue(claimant, out Job outJob))
                    {
                        if (job == outJob)
                        {
                            pawnToJob.TryRemove(claimant, out _);
                            targetToPawnToJob.TryRemove(key, out _);
                        }
                    }                    
                }
            }
            
            return false;
        }

        public static bool ReleaseAllClaimedBy(PhysicalInteractionReservationManager __instance, Pawn claimant)
        {
            ConcurrentDictionary<LocalTargetInfo, ConcurrentDictionary<Pawn, Job>> targetToPawnToJob =
                instanceTargetToPawnToJob.GetOrAdd(__instance, new ConcurrentDictionary<LocalTargetInfo, ConcurrentDictionary<Pawn, Job>>());

            foreach (LocalTargetInfo key in targetToPawnToJob.Keys.ToList())
            {
                targetToPawnToJob.TryGetValue(key, out ConcurrentDictionary<Pawn, Job> pawnToJob);
                if (pawnToJob.ContainsKey(claimant))
                {
                    pawnToJob.TryRemove(claimant, out _);
                    targetToPawnToJob.TryRemove(key, out _);
                }
            }
            return false;
        }

    }
}

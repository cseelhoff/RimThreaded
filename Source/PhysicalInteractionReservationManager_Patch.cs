using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace RimThreaded
{
    public class PhysicalInteractionReservationManager_Patch
    {
        public static Dictionary<PhysicalInteractionReservationManager, Dictionary<LocalTargetInfo, Dictionary<Pawn, Job>>> instanceTargetToPawnToJob = new Dictionary<PhysicalInteractionReservationManager, Dictionary<LocalTargetInfo, Dictionary<Pawn, Job>>>();
        public static Dictionary<PhysicalInteractionReservationManager, Dictionary<Pawn, Dictionary<LocalTargetInfo, Job>>> instancePawnToTargetToJob = new Dictionary<PhysicalInteractionReservationManager, Dictionary<Pawn, Dictionary<LocalTargetInfo, Job>>>();

        public static void RunDestructivePatches()
        {
            Type original = typeof(PhysicalInteractionReservationManager);
            Type patched = typeof(PhysicalInteractionReservationManager_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(IsReservedBy));
            RimThreadedHarmony.Prefix(original, patched, nameof(Reserve));
            RimThreadedHarmony.Prefix(original, patched, nameof(Release));
            RimThreadedHarmony.Prefix(original, patched, nameof(FirstReserverOf));
            RimThreadedHarmony.Prefix(original, patched, nameof(FirstReservationFor));
            RimThreadedHarmony.Prefix(original, patched, nameof(ReleaseAllForTarget));
            RimThreadedHarmony.Prefix(original, patched, nameof(ReleaseClaimedBy));
            RimThreadedHarmony.Prefix(original, patched, nameof(ReleaseAllClaimedBy));
        }


        public static bool IsReservedBy(PhysicalInteractionReservationManager __instance, ref bool __result, Pawn claimant, LocalTargetInfo target)
        {
            __result = false;
            if (instanceTargetToPawnToJob.TryGetValue(__instance, out Dictionary<LocalTargetInfo, Dictionary<Pawn, Job>> targetToPawnToJob))
            {
                if (targetToPawnToJob.TryGetValue(target, out Dictionary<Pawn, Job> pawnToJob))
                    __result = pawnToJob.ContainsKey(claimant);
            }
            return false;
        }

        public static bool Reserve(PhysicalInteractionReservationManager __instance, Pawn claimant, Job job, LocalTargetInfo target)
        {
            if (!instanceTargetToPawnToJob.TryGetValue(__instance, out Dictionary<LocalTargetInfo, Dictionary<Pawn, Job>> targetToPawnToJob))
            {
                lock (__instance)
                {
                    if (!instanceTargetToPawnToJob.TryGetValue(__instance, out Dictionary<LocalTargetInfo, Dictionary<Pawn, Job>> targetToPawnToJob2))
                    {
                        targetToPawnToJob2 = new Dictionary<LocalTargetInfo, Dictionary<Pawn, Job>>();
                        instanceTargetToPawnToJob.Add(__instance, targetToPawnToJob2);
                    }
                    targetToPawnToJob = targetToPawnToJob2;
                }
            }
            if (!targetToPawnToJob.TryGetValue(target, out Dictionary<Pawn, Job> pawnToJob))
            {
                lock (__instance)
                {
                    if (!targetToPawnToJob.TryGetValue(target, out Dictionary<Pawn, Job> pawnToJob2))
                    {
                        pawnToJob2 = new Dictionary<Pawn, Job>();
                        targetToPawnToJob.Add(target, pawnToJob2);
                    }
                    pawnToJob = pawnToJob2;
                }
            }
            if (!instancePawnToTargetToJob.TryGetValue(__instance, out Dictionary<Pawn, Dictionary<LocalTargetInfo, Job>> pawnToTargetToJob))
            {
                lock (__instance)
                {
                    if (!instancePawnToTargetToJob.TryGetValue(__instance, out Dictionary<Pawn, Dictionary<LocalTargetInfo, Job>> pawnToTargetToJob2))
                    {
                        pawnToTargetToJob2 = new Dictionary<Pawn, Dictionary<LocalTargetInfo, Job>>();
                        instancePawnToTargetToJob.Add(__instance, pawnToTargetToJob2);
                    }
                    pawnToTargetToJob = pawnToTargetToJob2;
                }
            }
            if (!pawnToTargetToJob.TryGetValue(claimant, out Dictionary<LocalTargetInfo, Job> targetToJob))
            {
                lock (__instance)
                {
                    if (!pawnToTargetToJob.TryGetValue(claimant, out Dictionary<LocalTargetInfo, Job> targetToJob2))
                    {
                        targetToJob2 = new Dictionary<LocalTargetInfo, Job>();
                        pawnToTargetToJob.Add(claimant, targetToJob2);
                    }
                    targetToJob = targetToJob2;
                }
            }

            lock (__instance) {
                pawnToJob.Add(claimant, job);
                targetToJob.Add(target, job);
            }
            return false;
        }

        public static bool Release(PhysicalInteractionReservationManager __instance, Pawn claimant, Job job, LocalTargetInfo target)
        {
            bool plantReregistered = false;
            lock (__instance)
            {
                if (instanceTargetToPawnToJob.TryGetValue(__instance, out Dictionary<LocalTargetInfo, Dictionary<Pawn, Job>> targetToPawnToJob)) {
                    if (targetToPawnToJob.TryGetValue(target, out Dictionary<Pawn, Job> pawnToJob)) {
                        if (pawnToJob.TryGetValue(claimant, out Job outJob)) {
                            if (outJob == job) {
                                pawnToJob.Remove(claimant);
                                plantReregistered = true;
                                PlantHarvest_Cache.ReregisterObject(claimant.Map, target.Cell, PlantHarvest_Cache.awaitingHarvestCellsMapDict);
                            }
                            else
                            {
                                Log.Warning(claimant.ToString() + " tried to release reservation on target " + target + ", but job was different.");
                            }
                        } else
                        {
                            Log.Warning(claimant.ToString() + " tried to release reservation on target " + target + ", but job was different.");
                        }
                    }
                    else
                    {
                        Log.Warning(claimant.ToString() + " tried to release reservation on target " + target + ", but claimant was not found.");
                    }
                }
                else
                {
                    Log.Warning(claimant.ToString() + " tried to release reservation on target " + target + ", but target had no physical reservations.");
                }
                if (instancePawnToTargetToJob.TryGetValue(__instance, out Dictionary<Pawn, Dictionary<LocalTargetInfo, Job>> pawnToTargetToJob))
                {
                    if (pawnToTargetToJob.TryGetValue(claimant, out Dictionary<LocalTargetInfo, Job> targetToJob))
                    {
                        if (targetToJob.TryGetValue(target, out Job outJob2))
                        {
                            if (outJob2 == job)
                            {
                                bool targetToJobResult = targetToJob.Remove(target);
                                if (!plantReregistered)
                                    PlantHarvest_Cache.ReregisterObject(claimant.Map, target.Cell, PlantHarvest_Cache.awaitingHarvestCellsMapDict);
                            }
                            else
                            {
                                Log.Warning(claimant.ToString() + " tried to release reservation on target " + target + ", but job was different.");
                            }
                        }
                        else
                        {
                            Log.Warning(claimant.ToString() + " tried to release reservation on target " + target + ", but job was different.");
                        }
                    }
                    else
                    {
                        Log.Warning(claimant.ToString() + " tried to release reservation on target " + target + ", but claimant was not found.");
                    }
                }
                else
                {
                    Log.Warning(claimant.ToString() + " tried to release reservation on target " + target + ", but target had no physical reservations.");
                }
            }
            return false;
        }


        public static bool FirstReserverOf(PhysicalInteractionReservationManager __instance, ref Pawn __result, LocalTargetInfo target)
        {
            __result = null;
            if (instanceTargetToPawnToJob.TryGetValue(__instance, out Dictionary<LocalTargetInfo, Dictionary<Pawn, Job>> targetToPawnToJob))
            {
                if (targetToPawnToJob.TryGetValue(target, out Dictionary<Pawn, Job> pawnToJob) && pawnToJob.Count > 0)
                {
                    try
                    {
                        __result = pawnToJob.First().Key;
                    }
                    catch (InvalidOperationException)
                    {
                    }
                }
            }
            return false;
        }

        public static bool FirstReservationFor(PhysicalInteractionReservationManager __instance, ref LocalTargetInfo __result, Pawn claimant)
        {
            __result = LocalTargetInfo.Invalid;
            if (instancePawnToTargetToJob.TryGetValue(__instance, out Dictionary<Pawn, Dictionary<LocalTargetInfo, Job>> pawnToTargetToJob))
            {
                if (pawnToTargetToJob.TryGetValue(claimant, out Dictionary<LocalTargetInfo, Job> targetToJob) && targetToJob.Count > 0)
                {
                    try
                    {
                        __result = targetToJob.First().Key;
                    }
                    catch (InvalidOperationException)
                    {
                    }
                }
            }
            return false;
        }

        public static bool ReleaseAllForTarget(PhysicalInteractionReservationManager __instance, LocalTargetInfo target)
        {
            if (instanceTargetToPawnToJob.TryGetValue(__instance, out Dictionary<LocalTargetInfo, Dictionary<Pawn, Job>> targetToPawnToJob))
            {
                if (targetToPawnToJob.TryGetValue(target, out _))
                {
                    lock (__instance)
                    {
                        if (targetToPawnToJob.TryGetValue(target, out Dictionary<Pawn, Job> pawnToJob))
                        {
                            foreach (KeyValuePair<Pawn, Job> kvp in pawnToJob) {
                                if (instancePawnToTargetToJob.TryGetValue(__instance, out Dictionary<Pawn, Dictionary<LocalTargetInfo, Job>> pawnToTargetToJob))
                                {
                                    Pawn pawn = kvp.Key;
                                    if (pawnToTargetToJob.TryGetValue(pawn, out Dictionary<LocalTargetInfo, Job> targetToJob))
                                    {
                                        if (targetToJob.TryGetValue(target, out _))
                                        {
                                            targetToJob.Remove(target);
                                        }
                                    }
                                }
                            }
                            targetToPawnToJob.Remove(target);
                            if (target != null && target.Thing != null && target.Thing.Map != null)
                                PlantHarvest_Cache.ReregisterObject(target.Thing.Map, target.Cell, PlantHarvest_Cache.awaitingHarvestCellsMapDict);
                        }
                    }
                }
            }
            return false;
        }

        public static bool ReleaseClaimedBy(PhysicalInteractionReservationManager __instance, Pawn claimant, Job job)
        {
            if (instancePawnToTargetToJob.TryGetValue(__instance, out Dictionary<Pawn, Dictionary<LocalTargetInfo, Job>> pawnToTargetToJob))
            {
                if (pawnToTargetToJob.TryGetValue(claimant, out Dictionary<LocalTargetInfo, Job> targetToJob) && targetToJob.Count > 0)
                {
                    lock (__instance)
                    {
                        foreach (KeyValuePair<LocalTargetInfo, Job> kvp in targetToJob)
                        {
                            if (kvp.Value == job)
                            {
                                LocalTargetInfo localTargetInfo = kvp.Key;
                                if (instanceTargetToPawnToJob.TryGetValue(__instance, out Dictionary<LocalTargetInfo, Dictionary<Pawn, Job>> targetToPawnToJob))
                                {
                                    if (targetToPawnToJob.TryGetValue(localTargetInfo, out Dictionary<Pawn, Job> pawnToJob))
                                    {
                                        if (pawnToJob.TryGetValue(claimant, out Job job2))
                                        {
                                            if (job == job2)
                                            {
                                                pawnToJob.Remove(claimant);
                                            }
                                        }
                                    }
                                }
                                targetToJob.Remove(localTargetInfo);
                                PlantHarvest_Cache.ReregisterObject(claimant.Map, localTargetInfo.Cell, PlantHarvest_Cache.awaitingHarvestCellsMapDict);
                            }
                        }
                    }
                }
            }
            
            return false;
        }

        public static bool ReleaseAllClaimedBy(PhysicalInteractionReservationManager __instance, Pawn claimant)
        {
            if (instancePawnToTargetToJob.TryGetValue(__instance, out Dictionary<Pawn, Dictionary<LocalTargetInfo, Job>> pawnToTargetToJob))
            {
                if (pawnToTargetToJob.TryGetValue(claimant, out Dictionary<LocalTargetInfo, Job> targetToJob) && targetToJob.Count > 0)
                {
                    lock (__instance)
                    {
                        foreach (KeyValuePair<LocalTargetInfo, Job> kvp in targetToJob)
                        {
                            LocalTargetInfo localTargetInfo = kvp.Key;
                            if (instanceTargetToPawnToJob.TryGetValue(__instance, out Dictionary<LocalTargetInfo, Dictionary<Pawn, Job>> targetToPawnToJob))
                            {
                                if (targetToPawnToJob.TryGetValue(localTargetInfo, out Dictionary<Pawn, Job> pawnToJob))
                                {
                                    if (pawnToJob.TryGetValue(claimant, out _))
                                    {
                                        pawnToJob.Remove(claimant);
                                        PlantHarvest_Cache.ReregisterObject(claimant.Map, localTargetInfo.Cell, PlantHarvest_Cache.awaitingHarvestCellsMapDict);
                                    }
                                }
                            }
                        }
                        pawnToTargetToJob.Remove(claimant);
                    }
                }
            }
            return false;
        }

    }
}

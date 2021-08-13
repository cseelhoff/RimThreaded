using RimWorld;
using System;
using Verse;
using Verse.AI;

namespace RimThreaded
{

    public class Pawn_JobTracker_Patch
    {

        static object determineNextJobLock = new object();

        internal static void RunDestructivePatches()
        {
            Type original = typeof(Pawn_JobTracker);
            Type patched = typeof(Pawn_JobTracker_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(TryFindAndStartJob));
            RimThreadedHarmony.Prefix(original, patched, nameof(Notify_DamageTaken));
            RimThreadedHarmony.Prefix(original, patched, nameof(DetermineNextJob));
            RimThreadedHarmony.Prefix(original, patched, nameof(StartJob)); //conflict with giddyupcore calling MakeDriver
        }

        public static bool StartJob(Pawn_JobTracker __instance,
          Job newJob,
          JobCondition lastJobEndCondition = JobCondition.None,
          ThinkNode jobGiver = null,
          bool resumeCurJobAfterwards = false,
          bool cancelBusyStances = true,
          ThinkTreeDef thinkTree = null,
          JobTag? tag = null,
          bool fromQueue = false,
          bool canReturnCurJobToPool = false)
        {
            __instance.startingNewJob = true;
            try
            {
                if (!fromQueue && (!Find.TickManager.Paused || __instance.lastJobGivenAtFrame == RealTime.frameCount))
                {
                    __instance.jobsGivenThisTick++;
                    if (Prefs.DevMode)
                    {
                        __instance.jobsGivenThisTickTextual = __instance.jobsGivenThisTickTextual + "(" + newJob.ToString() + ") ";
                    }
                }

                __instance.lastJobGivenAtFrame = RealTime.frameCount;
                if (__instance.jobsGivenThisTick > 10)
                {
                    string text = __instance.jobsGivenThisTickTextual;
                    __instance.jobsGivenThisTick = 0;
                    __instance.jobsGivenThisTickTextual = "";
                    __instance.startingNewJob = false;
                    __instance.pawn.ClearReservationsForJob(newJob);
                    JobUtility.TryStartErrorRecoverJob(__instance.pawn, __instance.pawn.ToStringSafe() + " started 10 jobs in one tick. newJob=" + newJob.ToStringSafe() + " jobGiver=" + jobGiver.ToStringSafe() + " jobList=" + text);
                    return false;
                }

                if (__instance.debugLog)
                {
                    __instance.DebugLogEvent(string.Concat("StartJob [", newJob, "] lastJobEndCondition=", lastJobEndCondition, ", jobGiver=", jobGiver, ", cancelBusyStances=", cancelBusyStances.ToString()));
                }
                var stances = __instance.pawn.stances; //changed
                if (cancelBusyStances && stances != null && stances.FullBodyBusy) //changed
                {
                    stances.CancelBusyStanceHard(); //changed
                }

                if (__instance.curJob != null)
                {
                    if (lastJobEndCondition == JobCondition.None)
                    {
                        Log.Warning(string.Concat(__instance.pawn, " starting job ", newJob, " from JobGiver ", newJob.jobGiver, " while already having job ", __instance.curJob, " without a specific job end condition."));
                        lastJobEndCondition = JobCondition.InterruptForced;
                    }

                    if (resumeCurJobAfterwards && __instance.curJob.def.suspendable)
                    {
                        __instance.jobQueue.EnqueueFirst(__instance.curJob);
                        if (__instance.debugLog)
                        {
                            __instance.DebugLogEvent("   JobQueue EnqueueFirst curJob: " + __instance.curJob);
                        }

                        __instance.CleanupCurrentJob(lastJobEndCondition, releaseReservations: false, cancelBusyStances);
                    }
                    else
                    {
                        __instance.CleanupCurrentJob(lastJobEndCondition, releaseReservations: true, cancelBusyStances, canReturnCurJobToPool);
                    }
                }

                if (newJob == null)
                {
                    Log.Warning(string.Concat(__instance.pawn, " tried to start doing a null job."));
                    return false;
                }

                newJob.startTick = Find.TickManager.TicksGame;
                if (__instance.pawn.Drafted || newJob.playerForced)
                {
                    newJob.ignoreForbidden = true;
                    newJob.ignoreDesignations = true;
                }

                __instance.curJob = newJob;
                __instance.curJob.jobGiverThinkTree = thinkTree;
                __instance.curJob.jobGiver = jobGiver;
                JobDriver cDriver = __instance.curJob.MakeDriver(__instance.pawn); //changed
                __instance.curDriver = cDriver; //changed
                bool flag = fromQueue;
                if (__instance.curDriver.TryMakePreToilReservations(!flag))
                {
                    Job job = __instance.TryOpportunisticJob(newJob);
                    if (job != null)
                    {
                        __instance.jobQueue.EnqueueFirst(newJob);
                        __instance.curJob = null;
                        __instance.curDriver = null;
                        __instance.StartJob(job);
                        return false;
                    }
#if RW13
                    if (tag.HasValue)
                    {
                        if (tag == JobTag.Fieldwork && __instance.pawn.mindState.lastJobTag != tag)
                        {
                            foreach (Pawn item in PawnUtility.SpawnedMasteredPawns(__instance.pawn))
                            {
                                item.jobs.Notify_MasterStartedFieldWork();
                            }
                        }
                        __instance.pawn.mindState.lastJobTag = tag.Value;
                    }

                    if (!__instance.pawn.Destroyed && __instance.pawn.ShouldDropCarriedThingBeforeJob(__instance.curJob))
                    {
                        if (DebugViewSettings.logCarriedBetweenJobs)
                        {
                            Log.Message($"Dropping {__instance.pawn.carryTracker.CarriedThing} before starting job {newJob}");
                        }

                        __instance.pawn.carryTracker.TryDropCarriedThing(__instance.pawn.Position, ThingPlaceMode.Near, out Thing _);
                    }                    
#endif
                    cDriver.SetInitialPosture(); //changed
                    cDriver.Notify_Starting(); //changed
                    cDriver.SetupToils(); //changed
                    cDriver.ReadyForNextToil(); //changed
                }
                else if (flag)
                {
                    __instance.EndCurrentJob(JobCondition.QueuedNoLongerValid);
                }
                else
                {
                    Log.Warning("TryMakePreToilReservations() returned false for a non-queued job right after StartJob(). This should have been checked before. curJob=" + __instance.curJob.ToStringSafe());
                    __instance.EndCurrentJob(JobCondition.Errored);
                }
            }
            finally
            {
                __instance.startingNewJob = false;
            }
            return false;
        }


        public static bool DetermineNextJob(Pawn_JobTracker __instance, ref ThinkResult __result, out ThinkTreeDef thinkTree)
        {
            ThinkResult constantThinkTreeJob = __instance.DetermineNextConstantThinkTreeJob();
            if (constantThinkTreeJob.Job != null)
            {
                thinkTree = __instance.pawn.thinker.ConstantThinkTree;
                __result = constantThinkTreeJob;
                return false;
            }
            ThinkResult thinkResult = ThinkResult.NoJob;
            try
            {
                thinkResult = __instance.pawn.thinker.MainThinkNodeRoot.TryIssueJobPackage(__instance.pawn, new JobIssueParams());
            }
            catch (Exception ex)
            {
                JobUtility.TryStartErrorRecoverJob(__instance.pawn, __instance.pawn.ToStringSafe<Pawn>() + " threw exception while determining job (main)", ex);
                thinkTree = null;
                __result = ThinkResult.NoJob;
                return false;
            }
            finally
            {
            }
            thinkTree = __instance?.pawn?.thinker?.MainThinkTree; //changed
            if (thinkTree == null) //changed
                thinkResult = ThinkResult.NoJob; //changed
            __result = thinkResult;
            return false;
        }

        public static bool Notify_DamageTaken(Pawn_JobTracker __instance, DamageInfo dinfo)
        {
            Job curJob = __instance.curJob;
            if (curJob == null)
                return false;
            JobDriver curDriver = __instance.curDriver;
            if (curDriver == null)
                return false;
            curDriver.Notify_DamageTaken(dinfo);
            Job curJob2 = __instance.curJob;
            if (curJob2 != curJob || !dinfo.Def.ExternalViolenceFor(__instance.pawn) || (!dinfo.Def.canInterruptJobs || curJob2.playerForced) || Find.TickManager.TicksGame < __instance.lastDamageCheckTick + 180)
                return false;
            Thing instigator = dinfo.Instigator;
            if (curJob2.def.checkOverrideOnDamage != CheckJobOverrideOnDamageMode.Always && (curJob2.def.checkOverrideOnDamage != CheckJobOverrideOnDamageMode.OnlyIfInstigatorNotJobTarget || __instance.curJob.AnyTargetIs((LocalTargetInfo)instigator)))
                return false;
            __instance.lastDamageCheckTick = Find.TickManager.TicksGame;
            __instance.CheckForJobOverride();
            return false;
        }

        public static bool TryFindAndStartJob(Pawn_JobTracker __instance)
        {
            if (__instance.pawn.thinker == null)
            {
                Log.ErrorOnce(string.Concat(__instance.pawn, " did TryFindAndStartJob but had no thinker."), 8573261);
                return false;
            }

            if (__instance.curJob != null)
            {
                Log.Warning(string.Concat(__instance.pawn, " doing TryFindAndStartJob while still having job ", __instance.curJob));
            }

            if (__instance.debugLog)
            {
                __instance.DebugLogEvent("TryFindAndStartJob");
            }

            if (!__instance.CanDoAnyJob())
            {
                if (__instance.debugLog)
                {
                    __instance.DebugLogEvent("   CanDoAnyJob is false. Clearing queue and returning");
                }

                __instance.ClearQueuedJobs();
                return false;
            }
            ThinkTreeDef thinkTree;
            lock (determineNextJobLock) //TODO change to ReservationManager.reservations?
            {
                ThinkResult result = DetermineNextJob2(__instance, out thinkTree);
                if (result.IsValid)
                {
                    __instance.CheckLeaveJoinableLordBecauseJobIssued(result);
                    __instance.StartJob(result.Job, JobCondition.None, result.SourceNode, resumeCurJobAfterwards: false, cancelBusyStances: false, thinkTree, result.Tag, result.FromQueue);
                }
            }
            return false;
        }
        private static ThinkResult DetermineNextJob2(Pawn_JobTracker __instance, out ThinkTreeDef thinkTree)
        {
            ThinkResult result = __instance.DetermineNextConstantThinkTreeJob();
            if (result.Job != null)
            {
                thinkTree = __instance.pawn.thinker.ConstantThinkTree;
                return result;
            }

            ThinkResult result2 = ThinkResult.NoJob;
            try
            {
                result2 = __instance.pawn.thinker.MainThinkNodeRoot.TryIssueJobPackage(__instance.pawn, default);
            }
            catch (Exception exception)
            {
                JobUtility.TryStartErrorRecoverJob(__instance.pawn, __instance.pawn.ToStringSafe() + " threw exception while determining job (main)", exception);
                thinkTree = null;
                return ThinkResult.NoJob;
            }
            finally
            {
            }

            thinkTree = __instance.pawn.thinker.MainThinkTree;
            return result2;
        }

    }
}

using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using static HarmonyLib.AccessTools;
using System.Reflection;

namespace RimThreaded
{

    public class Pawn_JobTracker_Patch
    {

        public static FieldRef<Pawn_JobTracker, Pawn> pawnFieldRef = FieldRefAccess<Pawn_JobTracker, Pawn>("pawn");
        static readonly MethodInfo methodCanDoAnyJob = Method(typeof(Pawn_JobTracker), "CanDoAnyJob", new Type[] { });
        static readonly Func<Pawn_JobTracker, bool> funcCanDoAnyJob = (Func<Pawn_JobTracker, bool>)Delegate.CreateDelegate(typeof(Func<Pawn_JobTracker, bool>), methodCanDoAnyJob);
        static readonly MethodInfo methodDetermineNextConstantThinkTreeJob = Method(typeof(Pawn_JobTracker), "DetermineNextConstantThinkTreeJob", new Type[] { });
        static readonly Func<Pawn_JobTracker, ThinkResult> funcDetermineNextConstantThinkTreeJob = (Func<Pawn_JobTracker, ThinkResult>)Delegate.CreateDelegate(typeof(Func<Pawn_JobTracker, ThinkResult>), methodDetermineNextConstantThinkTreeJob);
        static MethodInfo methodCheckLeaveJoinableLordBecauseJobIssued = Method(typeof(Pawn_JobTracker), "CheckLeaveJoinableLordBecauseJobIssued");
        static Action<Pawn_JobTracker, ThinkResult> actionCheckLeaveJoinableLordBecauseJobIssued = (Action<Pawn_JobTracker, ThinkResult>)Delegate.CreateDelegate(typeof(Action<Pawn_JobTracker, ThinkResult>), methodCheckLeaveJoinableLordBecauseJobIssued);



        static object determineNextJobLock = new object();
        private static int lastJobGivenAtFrame = -1;
        private static int jobsGivenThisTick;
        private static string jobsGivenThisTickTextual = "";
        

        public static bool TryFindAndStartJob(Pawn_JobTracker __instance)
        {
            if (pawnFieldRef(__instance).thinker == null)
            {
                Log.ErrorOnce(string.Concat(pawnFieldRef(__instance), " did TryFindAndStartJob but had no thinker."), 8573261);
                return false;
            }

            if (__instance.curJob != null)
            {
                Log.Warning(string.Concat(pawnFieldRef(__instance), " doing TryFindAndStartJob while still having job ", __instance.curJob));
            }

            if (__instance.debugLog)
            {
                __instance.DebugLogEvent("TryFindAndStartJob");
            }

            if (!funcCanDoAnyJob(__instance))
            { 
                if (__instance.debugLog)
                {
                    __instance.DebugLogEvent("   CanDoAnyJob is false. Clearing queue and returning");
                }

                __instance.ClearQueuedJobs();
                return false;
            }

            lock (determineNextJobLock) //TODO change to ReservationManager.reservations?
            {
                ThinkResult result = DetermineNextJob2(__instance, out ThinkTreeDef thinkTree);

                if (result.IsValid)
                {
                    actionCheckLeaveJoinableLordBecauseJobIssued(__instance, result);
                    __instance.StartJob(result.Job, JobCondition.None, result.SourceNode, resumeCurJobAfterwards: false, cancelBusyStances: false, thinkTree, result.Tag, result.FromQueue);
                    //StartJob(__instance, result.Job, JobCondition.None, result.SourceNode, false, false, thinkTree, result.Tag, result.FromQueue);
                }
            }
            return false;
        }
        private static ThinkResult DetermineNextJob2(Pawn_JobTracker __instance, out ThinkTreeDef thinkTree)
        {
            ThinkResult result = funcDetermineNextConstantThinkTreeJob(__instance);
            if (result.Job != null)
            {
                thinkTree = pawnFieldRef(__instance).thinker.ConstantThinkTree;
                return result;
            }

            ThinkResult result2 = ThinkResult.NoJob;
            try
            {
                result2 = pawnFieldRef(__instance).thinker.MainThinkNodeRoot.TryIssueJobPackage(pawnFieldRef(__instance), default(JobIssueParams));
            }
            catch (Exception exception)
            {
                JobUtility.TryStartErrorRecoverJob(pawnFieldRef(__instance), pawnFieldRef(__instance).ToStringSafe() + " threw exception while determining job (main)", exception);
                thinkTree = null;
                return ThinkResult.NoJob;
            }

            thinkTree = pawnFieldRef(__instance).thinker.MainThinkTree;
            return result2;
        }

        private static void CleanupCurrentJob(Pawn_JobTracker __instance, JobCondition condition, bool releaseReservations, bool cancelBusyStancesSoft = true, bool canReturnToPool = false)
        {
            if (__instance.debugLog)
            {
                __instance.DebugLogEvent(string.Concat(new object[]
                {
                    "CleanupCurrentJob ",
                    (__instance.curJob != null) ? __instance.curJob.def.ToString() : "null",
                    " condition ",
                    condition
                }));
            }
            if (__instance.curJob == null)
            {
                return;
            }
            if (releaseReservations)
            {
                pawnFieldRef(__instance).ClearReservationsForJob(__instance.curJob);
            }
            if (__instance.curDriver != null)
            {
                __instance.curDriver.ended = true;
                __instance.curDriver.Cleanup(condition);
            }
            __instance.curDriver = null;
            Job job = __instance.curJob;
            __instance.curJob = null;
            pawnFieldRef(__instance).VerifyReservations();
            if (cancelBusyStancesSoft)
            {
                pawnFieldRef(__instance).stances.CancelBusyStanceSoft();
            }
            if (!pawnFieldRef(__instance).Destroyed && pawnFieldRef(__instance).carryTracker != null && pawnFieldRef(__instance).carryTracker.CarriedThing != null)
            {
                Thing thing;
                pawnFieldRef(__instance).carryTracker.TryDropCarriedThing(pawnFieldRef(__instance).Position, ThingPlaceMode.Near, out thing, null);
            }
            if (releaseReservations && canReturnToPool)
            {
                JobMaker.ReturnToPool(job);
            }
        }

        public static bool StartJob(Pawn_JobTracker __instance, Job newJob, JobCondition lastJobEndCondition = JobCondition.None, ThinkNode jobGiver = null, bool resumeCurJobAfterwards = false, bool cancelBusyStances = true, ThinkTreeDef thinkTree = null, JobTag? tag = null, bool fromQueue = false, bool canReturnCurJobToPool = false)
        {
            __instance.startingNewJob = true;
            try
            {
                if (!fromQueue && (!Find.TickManager.Paused || lastJobGivenAtFrame == RealTime.frameCount))
                {
                    jobsGivenThisTick++;
                    if (Prefs.DevMode)
                    {
                        jobsGivenThisTickTextual = jobsGivenThisTickTextual + "(" + newJob + ") ";
                    }
                }
                lastJobGivenAtFrame = RealTime.frameCount;
                if (jobsGivenThisTick > 10)
                {
                    string text = jobsGivenThisTickTextual;
                    jobsGivenThisTick = 0;
                    jobsGivenThisTickTextual = "";
                    __instance.startingNewJob = false;
                    pawnFieldRef(__instance).ClearReservationsForJob(newJob);
                    JobUtility.TryStartErrorRecoverJob(pawnFieldRef(__instance), string.Concat(new string[]
                    {
                        pawnFieldRef(__instance).ToStringSafe<Pawn>(),
                        " started 10 jobs in one tick. newJob=",
                        newJob.ToStringSafe<Job>(),
                        " jobGiver=",
                        jobGiver.ToStringSafe<ThinkNode>(),
                        " jobList=",
                        text
                    }), null, null);
                }
                else
                {
                    if (__instance.debugLog)
                    {
                        __instance.DebugLogEvent(string.Concat(new object[]
                        {
                            "StartJob [",
                            newJob,
                            "] lastJobEndCondition=",
                            lastJobEndCondition,
                            ", jobGiver=",
                            jobGiver,
                            ", cancelBusyStances=",
                            cancelBusyStances.ToString()
                        }));
                    }
                    if (cancelBusyStances && pawnFieldRef(__instance).stances.FullBodyBusy)
                    {
                        pawnFieldRef(__instance).stances.CancelBusyStanceHard();
                    }
                    if (__instance.curJob != null)
                    {
                        if (lastJobEndCondition == JobCondition.None)
                        {
                            Log.Warning(string.Concat(new object[]
                            {
                                pawnFieldRef(__instance),
                                " starting job ",
                                newJob,
                                " from JobGiver ",
                                newJob.jobGiver,
                                " while already having job ",
                                __instance.curJob,
                                " without a specific job end condition."
                            }), false);
                            lastJobEndCondition = JobCondition.InterruptForced;
                        }
                        if (resumeCurJobAfterwards && __instance.curJob.def.suspendable)
                        {
                            __instance.jobQueue.EnqueueFirst(__instance.curJob, null);
                            if (__instance.debugLog)
                            {
                                __instance.DebugLogEvent("   JobQueue EnqueueFirst curJob: " + __instance.curJob);
                            }
                            CleanupCurrentJob(__instance, lastJobEndCondition, false, cancelBusyStances, false);
                        }
                        else
                        {
                            CleanupCurrentJob(__instance, lastJobEndCondition, true, cancelBusyStances, canReturnCurJobToPool);
                        }
                    }
                    if (newJob == null)
                    {
                        Log.Warning(pawnFieldRef(__instance) + " tried to start doing a null job.", false);
                    }
                    else
                    {
                        newJob.startTick = Find.TickManager.TicksGame;
                        if (pawnFieldRef(__instance).Drafted || newJob.playerForced)
                        {
                            newJob.ignoreForbidden = true;
                            newJob.ignoreDesignations = true;
                        }
                        __instance.curJob = newJob;
                        __instance.curJob.jobGiverThinkTree = thinkTree;
                        __instance.curJob.jobGiver = jobGiver;
                        __instance.curDriver = __instance.curJob.MakeDriver(pawnFieldRef(__instance));
                        if (__instance.curDriver.TryMakePreToilReservations(!fromQueue))
                        {
                            Job job = __instance.TryOpportunisticJob(newJob);
                            if (job != null)
                            {
                                __instance.jobQueue.EnqueueFirst(newJob, null);
                                __instance.curJob = null;
                                __instance.curDriver = null;
                                __instance.StartJob(job, JobCondition.None, null, false, true, null, null, false, false);
                            }
                            else
                            {
                                if (tag != null)
                                {
                                    pawnFieldRef(__instance).mindState.lastJobTag = tag.Value;
                                }
                                __instance.curDriver.SetInitialPosture();
                                __instance.curDriver.Notify_Starting();
                                //JobDriver_Patch.SetupToils(pawnFieldRef(__instance));
                                __instance.curDriver.ReadyForNextToil();
                            }
                        }
                        else if (fromQueue)
                        {
                            __instance.EndCurrentJob(JobCondition.QueuedNoLongerValid, true, true);
                        }
                        else
                        {
                            Log.Warning("TryMakePreToilReservations() returned false for a non-queued job right after StartJob(). This should have been checked before. curJob=" + __instance.curJob.ToStringSafe<Job>(), false);
                            __instance.EndCurrentJob(JobCondition.Errored, true, true);
                        }
                    }
                }
            }
            finally
            {
                __instance.startingNewJob = false;
            }

            return false;
        }
        public static bool EndCurrentJob(Pawn_JobTracker __instance, JobCondition condition, bool startNewJob = true, bool canReturnToPool = true)
        {
            if (__instance.debugLog)
            {
                __instance.DebugLogEvent(string.Concat(new object[]
                {
                    "EndCurrentJob ",
                    (__instance.curJob != null) ? __instance.curJob.ToString() : "null",
                    " condition=",
                    condition,
                    " curToil=",
                    (__instance.curDriver != null) ? __instance.curDriver.CurToilIndex.ToString() : "null_driver"
                }));
            }
            if (condition == JobCondition.Ongoing)
            {
                Log.Warning("Ending a job with Ongoing as the condition. This makes no sense.", false);
            }
            if (condition == JobCondition.Succeeded && __instance.curJob != null && __instance.curJob.def.taleOnCompletion != null)
            {
                TaleRecorder.RecordTale(__instance.curJob.def.taleOnCompletion, __instance.curDriver.TaleParameters());
            }
            JobDef jobDef = __instance.curJob?.def;
            CleanupCurrentJob(__instance, condition, true, true, canReturnToPool);
            if (startNewJob)
            {
                if (condition == JobCondition.ErroredPather || condition == JobCondition.Errored)
                {
                    __instance.StartJob(JobMaker.MakeJob(JobDefOf.Wait, 250, false), JobCondition.None, null, false, true, null, null, false, false);
                    return false;
                }
                if (condition == JobCondition.Succeeded && jobDef != null && jobDef != JobDefOf.Wait_MaintainPosture && !pawnFieldRef(__instance).pather.Moving)
                {
                    __instance.StartJob(JobMaker.MakeJob(JobDefOf.Wait_MaintainPosture, 1, false), JobCondition.None, null, false, false, null, null, false, false);
                    return false;
                }
                if (__instance.curJob?.def != null && __instance.curJob.playerForced || condition == JobCondition.InterruptForced) 
                {
                    TryFindAndStartJob(__instance);
                }
            }
            return false;
        }


        internal static void RunDestructivePatches()
        {
            Type original = typeof(Pawn_JobTracker);
            Type patched = typeof(Pawn_JobTracker_Patch);
            RimThreadedHarmony.Prefix(original, patched, "TryFindAndStartJob");
            //RimThreadedHarmony.Prefix(original, patched, "StartJob"); //conflict with giddyupcore calling MakeDriver
            RimThreadedHarmony.Prefix(original, patched, "EndCurrentJob");

        }
    }
}
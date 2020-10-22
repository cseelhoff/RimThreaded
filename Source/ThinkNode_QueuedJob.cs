using HarmonyLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Verse;
using Verse.AI;

namespace RimThreaded
{
    
    public class ThinkNode_QueuedJob_Patch
    {
        public static bool TryIssueJobPackage(ThinkNode_QueuedJob __instance, ref ThinkResult __result, Pawn pawn, JobIssueParams jobParams)
        {
            JobQueue jobQueue = pawn.jobs.jobQueue;
            if (pawn.Downed || jobQueue.AnyCanBeginNow(pawn, __instance.inBedOnly))
            {
                while (jobQueue.Count > 0 && !jobQueue.Peek().job.CanBeginNow(pawn, __instance.inBedOnly))
                {
                    QueuedJob queuedJob = jobQueue.Dequeue();
                    pawn.ClearReservationsForJob(queuedJob.job);
                    if (pawn.jobs.debugLog)
                    {
                        pawn.jobs.DebugLogEvent("   Throwing away queued job that I cannot begin now: " + queuedJob.job);
                    }
                }
            }

            if (jobQueue.Count > 0)
            {
                QueuedJob jqpeek = jobQueue.Peek();
                if (jqpeek != null)
                {
                    if (jqpeek.job.CanBeginNow(pawn, __instance.inBedOnly))
                    {
                        QueuedJob queuedJob2 = jobQueue.Dequeue();
                        if (pawn.jobs.debugLog)
                        {
                            pawn.jobs.DebugLogEvent("   Returning queued job: " + queuedJob2.job);
                        }

                        __result = new ThinkResult(queuedJob2.job, __instance, queuedJob2.tag, fromQueue: true);
                        return false;
                    }
                }
            }

            __result = ThinkResult.NoJob;
            return false;
        }
    }
    
}

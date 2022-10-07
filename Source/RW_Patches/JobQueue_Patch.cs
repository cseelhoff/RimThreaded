using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace RimThreaded.RW_Patches
{

    public class JobQueue_Patch
    {

        internal static void RunDestructivePatches()
        {
            Type original = typeof(JobQueue);
            Type patched = typeof(JobQueue_Patch);
            RimThreadedHarmony.Prefix(original, patched, "AnyCanBeginNow");
            RimThreadedHarmony.Prefix(original, patched, "EnqueueFirst");
            RimThreadedHarmony.Prefix(original, patched, "EnqueueLast");
            RimThreadedHarmony.Prefix(original, patched, "Contains");
            RimThreadedHarmony.Prefix(original, patched, "Extract");
            RimThreadedHarmony.Prefix(original, patched, "Dequeue");
        }
        public static bool AnyCanBeginNow(JobQueue __instance, ref bool __result, Pawn pawn, bool whileLyingDown)
        {
            List<QueuedJob> j = __instance.jobs;
            for (int i = 0; i < j.Count; i++)
            {
                QueuedJob queuedJob = j[i];
                if (null == queuedJob) continue;
                if (!queuedJob.job.CanBeginNow(pawn, whileLyingDown)) continue;
                __result = true;
                return false;
            }
            __result = false;
            return false;
        }
        public static bool EnqueueFirst(JobQueue __instance, Job j, JobTag? tag = null)
        {
            lock (__instance)
            {
                List<QueuedJob> newJobs = new List<QueuedJob>(__instance.jobs);
                newJobs.Insert(0, new QueuedJob(j, tag));
                __instance.jobs = newJobs;
            }
            return false;
        }

        public static bool EnqueueLast(JobQueue __instance, Job j, JobTag? tag = null)
        {
            lock (__instance)
            {
                __instance.jobs.Add(new QueuedJob(j, tag));
            }
            return false;
        }

        public static bool Contains(JobQueue __instance, ref bool __result, Job j)
        {
            List<QueuedJob> snapshotJobs = __instance.jobs;
            for (int i = 0; i < snapshotJobs.Count; i++)
            {
                QueuedJob jobi;
                try
                {
                    jobi = snapshotJobs[i];
                }
                catch (ArgumentOutOfRangeException)
                {
                    break;
                }
                if (jobi.job == j)
                {
                    __result = true;
                    return false;
                }
            }

            return false;
        }

        public static bool Extract(JobQueue __instance, ref QueuedJob __result, Job j)
        {
            lock (__instance)
            {
                int num = __instance.jobs.FindIndex((qj) => qj.job == j);
                if (num >= 0)
                {
                    QueuedJob result = __instance.jobs[num];
                    __instance.jobs.RemoveAt(num);
                    __result = result;
                    return false;
                }
            }
            __result = null;
            return false;
        }

        public static bool Dequeue(JobQueue __instance, ref QueuedJob __result)
        {
            QueuedJob result;
            lock (__instance)
            {
                if (__instance.jobs.NullOrEmpty())
                {
                    __result = null;
                    return false;
                }

                result = __instance.jobs[0];
                __instance.jobs.RemoveAt(0);
            }
            __result = result;
            return false;
        }

    }
}

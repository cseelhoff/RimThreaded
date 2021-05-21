using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{

    public class JobQueue_Patch
	{

        public static FieldRef<JobQueue, List<QueuedJob>> jobs = FieldRefAccess<JobQueue, List<QueuedJob>>("jobs");
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
            List<QueuedJob> j = jobs(__instance);
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
                List<QueuedJob> newJobs = new List<QueuedJob>(jobs(__instance));
                newJobs.Insert(0, new QueuedJob(j, tag));
                jobs(__instance) = newJobs;
            }
            return false;
        }

        public static bool EnqueueLast(JobQueue __instance, Job j, JobTag? tag = null)
        {
            lock (__instance)
            {
                jobs(__instance).Add(new QueuedJob(j, tag));
            }
            return false;
        }

        public static bool Contains(JobQueue __instance, ref bool __result, Job j)
        {
            List<QueuedJob> snapshotJobs = jobs(__instance);
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
                int num = jobs(__instance).FindIndex((QueuedJob qj) => qj.job == j);
                if (num >= 0)
                {
                    QueuedJob result = jobs(__instance)[num];
                    jobs(__instance).RemoveAt(num);
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
                if (jobs(__instance).NullOrEmpty())
                {
                    __result = null;
                    return false;
                }

                result = jobs(__instance)[0];
                jobs(__instance).RemoveAt(0);
            }
            __result = result;
            return false;
        }

    }
}

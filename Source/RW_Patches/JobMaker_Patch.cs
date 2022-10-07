using System;
using System.Collections.Concurrent;
using Verse;
using Verse.AI;

namespace RimThreaded.RW_Patches
{

    public static class JobMaker_Patch
    {
        public static ConcurrentStack<Job> jobStack = new ConcurrentStack<Job>();

        public static void RunDestructivePatches()
        {
            Type original = typeof(JobMaker);
            Type patched = typeof(JobMaker_Patch);
            RimThreadedHarmony.Prefix(original, patched, "MakeJob", new Type[] { });
            RimThreadedHarmony.Prefix(original, patched, "ReturnToPool");
        }

        public static bool MakeJob(ref Job __result)
        {
            if (!jobStack.TryPop(out Job job))
            {
                job = new Job();
            }
            job.loadID = Find.UniqueIDsManager.GetNextJobID();
            __result = job;
            return false;
        }

        public static bool ReturnToPool(Job job)
        {
            if (job == null || jobStack.Count >= 1000)
                return false;
            job.Clear();
            jobStack.Push(job);
            return false;
        }

    }

}

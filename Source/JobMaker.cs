using System.Collections.Concurrent;
using Verse;
using Verse.AI;

namespace RimThreaded
{
    
    public static class JobMaker_Patch
    {
        public static ConcurrentStack<Job> jobStack = new ConcurrentStack<Job>();

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

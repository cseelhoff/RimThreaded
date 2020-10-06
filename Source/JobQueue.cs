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

namespace RimThreaded
{

    public class JobQueue_Patch
	{
        public static FieldRef<JobQueue, List<QueuedJob>> jobs = FieldRefAccess<JobQueue, List<QueuedJob>>("jobs");
        public static bool AnyCanBeginNow(JobQueue __instance, ref bool __result, Pawn pawn, bool whileLyingDown)
        {
            for (int i = 0; i < jobs(__instance).Count; i++)
            {
                QueuedJob queuedJob = jobs(__instance)[i];
                if (null != queuedJob)
                {
                    if (queuedJob.job.CanBeginNow(pawn, whileLyingDown))
                    {
                        __result = true;
                        return false;
                    }
                }
            }
            __result = false;
            return false;
        }


    }
}

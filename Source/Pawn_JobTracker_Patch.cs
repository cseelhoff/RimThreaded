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
            RimThreadedHarmony.Prefix(original, patched, "TryFindAndStartJob");
            //RimThreadedHarmony.Prefix(original, patched, "StartJob"); conflict with giddyupcore calling MakeDriver
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

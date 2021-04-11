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
        static readonly MethodInfo methodCanDoAnyJob =
            Method(typeof(Pawn_JobTracker), "CanDoAnyJob", new Type[] { });
        static readonly Func<Pawn_JobTracker, bool> funcCanDoAnyJob =
            (Func<Pawn_JobTracker, bool>)Delegate.CreateDelegate(typeof(Func<Pawn_JobTracker, bool>), methodCanDoAnyJob);
        static readonly MethodInfo methodDetermineNextConstantThinkTreeJob =
            Method(typeof(Pawn_JobTracker), "DetermineNextConstantThinkTreeJob", new Type[] { });
        static readonly Func<Pawn_JobTracker, ThinkResult> funcDetermineNextConstantThinkTreeJob =
            (Func<Pawn_JobTracker, ThinkResult>)Delegate.CreateDelegate(typeof(Func<Pawn_JobTracker, ThinkResult>), methodDetermineNextConstantThinkTreeJob);
        static MethodInfo methodCheckLeaveJoinableLordBecauseJobIssued =
            Method(typeof(Pawn_JobTracker), "CheckLeaveJoinableLordBecauseJobIssued");
        static Action<Pawn_JobTracker, ThinkResult> actionCheckLeaveJoinableLordBecauseJobIssued =
            (Action<Pawn_JobTracker, ThinkResult>)Delegate.CreateDelegate(typeof(Action<Pawn_JobTracker, ThinkResult>), methodCheckLeaveJoinableLordBecauseJobIssued);
        static object determineNextJobLock = new object();

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
            ThinkTreeDef thinkTree;
            lock (determineNextJobLock) //TODO change to ReservationManager.reservations?
            {
                ThinkResult result = DetermineNextJob2(__instance, out thinkTree);
                if (result.IsValid)
                {
                    actionCheckLeaveJoinableLordBecauseJobIssued(__instance, result);
                    __instance.StartJob(result.Job, JobCondition.None, result.SourceNode, resumeCurJobAfterwards: false, cancelBusyStances: false, thinkTree, result.Tag, result.FromQueue);
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
            finally
            {
            }

            thinkTree = pawnFieldRef(__instance).thinker.MainThinkTree;
            return result2;
        }

        internal static void RunDestructivePatches()
        {
            Type original = typeof(Pawn_JobTracker);
            Type patched = typeof(Pawn_JobTracker_Patch);
            RimThreadedHarmony.Prefix(original, patched, "TryFindAndStartJob");
            //RimThreadedHarmony.Prefix(original, patched, "StartJob"); conflict with giddyupcore calling MakeDriver
        }
    }
}

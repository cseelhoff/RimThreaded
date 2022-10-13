using System;
using Verse;
using System.Collections.Concurrent;

namespace RimThreaded.RW_Patches
{

    public class LongEventHandler_Patch
    {

        public static ConcurrentQueue<Action> toExecuteWhenFinished2 = new ConcurrentQueue<Action>();

        internal static void RunDestructivePatches()
        {
            Type original = typeof(LongEventHandler);
            Type patched = typeof(LongEventHandler_Patch);
            RimThreadedHarmony.Prefix(original, patched, "ExecuteToExecuteWhenFinished");
            RimThreadedHarmony.Prefix(original, patched, "ExecuteWhenFinished");
        }
        public static void RunNonDestructivePatches()
        {
            Type original = typeof(LongEventHandler);
            Type patched = typeof(LongEventHandler_Patch);
            RimThreadedHarmony.Prefix(original, patched, "RunEventFromAnotherThread", null, false);
        }

        public static bool ExecuteToExecuteWhenFinished()
        {
            if (toExecuteWhenFinished2.Count > 0)
            {
                DeepProfiler.Start("ExecuteToExecuteWhenFinished()");
            }
            while (toExecuteWhenFinished2.TryDequeue(out Action action))
            {
                DeepProfiler.Start(action.Method.DeclaringType + " -> " + action.Method);
                try
                {
                    action();
                }
                catch (Exception arg)
                {
                    Log.Error("Could not execute post-long-event action. Exception: " + arg);
                }
                finally
                {
                    DeepProfiler.End();
                }
            }

            if (toExecuteWhenFinished2.Count > 0)
            {
                DeepProfiler.End();
            }

            LongEventHandler.toExecuteWhenFinished.Clear();
            return false;
        }
        public static bool ExecuteWhenFinished(Action action)
        {
            toExecuteWhenFinished2.Enqueue(action);
            return true;
        }

        public static bool RunEventFromAnotherThread(Action action)
        {
            RimThreaded.InitializeAllThreadStatics();
            return true;
        }

    }



}

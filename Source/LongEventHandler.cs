using System;
using System.Collections.Generic;
using Verse;
using System.Threading;
using System.Collections.Concurrent;
using System.Collections;
using UnityEngine;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{

	public class LongEventHandler_Patch
	{
		public static bool executingToExecuteWhenFinished =
			StaticFieldRefAccess<bool>(typeof(LongEventHandler), "executingToExecuteWhenFinished");
		public static List<Action> toExecuteWhenFinished =
			StaticFieldRefAccess<List<Action>>(typeof(LongEventHandler), "toExecuteWhenFinished");
		public static ConcurrentQueue<Action> toExecuteWhenFinished2 = new ConcurrentQueue<Action>();

		public static AsyncOperation levelLoadOp =
			StaticFieldRefAccess<AsyncOperation>(typeof(LongEventHandler), "levelLoadOp");
		public static object CurrentEventTextLock =
			StaticFieldRefAccess<object>(typeof(LongEventHandler), "CurrentEventTextLock");
		public static Vector2 StatusRectSize =
			StaticFieldRefAccess<Vector2>(typeof(LongEventHandler), "StatusRectSize");
		public static Thread eventThread =
			StaticFieldRefAccess<Thread>(typeof(LongEventHandler), "eventThread");
		public static Queue<object> eventQueue1 =
			StaticFieldRefAccess<Queue<object>>(typeof(LongEventHandler), "eventQueue");
		public static object currentEvent1 =
			StaticFieldRefAccess<object>(typeof(LongEventHandler), "currentEvent");

		public static Queue<QueuedLongEvent2> eventQueue = new Queue<QueuedLongEvent2>();

		public static bool initCopyComplete = false;

		public class QueuedLongEvent2
		{
			public Action eventAction;

			public IEnumerator eventActionEnumerator;

			public string levelToLoad;

			public string eventTextKey = "";

			public string eventText = "";

			public bool doAsynchronously;

			public Action<Exception> exceptionHandler;

			public bool alreadyDisplayed;

			public bool canEverUseStandardWindow = true;

			public bool showExtraUIInfo = true;

			public bool UseAnimatedDots
			{
				get
				{
					if (!doAsynchronously)
					{
						return eventActionEnumerator != null;
					}

					return true;
				}
			}

			public bool ShouldWaitUntilDisplayed
			{
				get
				{
					if (!alreadyDisplayed && UseStandardWindow)
					{
						return !eventText.NullOrEmpty();
					}

					return false;
				}
			}

			public bool UseStandardWindow
			{
				get
				{
					if (canEverUseStandardWindow && !doAsynchronously)
					{
						return eventActionEnumerator == null;
					}

					return false;
				}
			}
		}

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
                DeepProfiler.Start(action.Method.DeclaringType.ToString() + " -> " + action.Method.ToString());
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

			toExecuteWhenFinished.Clear();
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

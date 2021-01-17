using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using System.Threading;
using System.Collections.Concurrent;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using static HarmonyLib.AccessTools;
using System.Reflection;

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

		static MethodInfo methodDrawLongEventWindow =
			Method(typeof(LongEventHandler), "DrawLongEventWindow", new Type[] { typeof(Rect) });
		static Action<Rect> actionDrawLongEventWindow =
			(Action<Rect>)Delegate.CreateDelegate(typeof(Action<Rect>), methodDrawLongEventWindow);

		static MethodInfo methodDrawLongEventWindowContents =
			Method(typeof(LongEventHandler), "DrawLongEventWindowContents", new Type[] { typeof(Rect) });
		static Action<Rect> actionDrawLongEventWindowContents =
			(Action<Rect>)Delegate.CreateDelegate(typeof(Action<Rect>), methodDrawLongEventWindowContents);

		static MethodInfo methodUpdateCurrentEnumeratorEvent =
			Method(typeof(LongEventHandler), "UpdateCurrentEnumeratorEvent", new Type[] { });
		static Action actionUpdateCurrentEnumeratorEvent =
			(Action)Delegate.CreateDelegate(typeof(Action), methodUpdateCurrentEnumeratorEvent);

		static MethodInfo methodUpdateCurrentAsynchronousEvent =
			Method(typeof(LongEventHandler), "UpdateCurrentAsynchronousEvent", new Type[] { });
		static Action actionUpdateCurrentAsynchronousEvent =
			(Action)Delegate.CreateDelegate(typeof(Action), methodUpdateCurrentAsynchronousEvent);

		static MethodInfo methodUpdateCurrentSynchronousEvent =
			Method(typeof(LongEventHandler), "UpdateCurrentSynchronousEvent", new Type[] { typeof(bool).MakeByRefType() });
		//static Action<bool> actionUpdateCurrentSynchronousEvent =
		//(Action<bool>)Delegate.CreateDelegate(typeof(Action<bool>).MakeByRefType(), methodUpdateCurrentSynchronousEvent);

		static MethodInfo methodRunEventFromAnotherThread =
			Method(typeof(LongEventHandler), "RunEventFromAnotherThread", new Type[] { typeof(Action) });
		static Action<Action> actionRunEventFromAnotherThread =
			(Action<Action>)Delegate.CreateDelegate(typeof(Action<Action>), methodRunEventFromAnotherThread);

		static MethodInfo methodExecuteToExecuteWhenFinished =
			Method(typeof(LongEventHandler), "ExecuteToExecuteWhenFinished", new Type[] { });
		static Action actionExecuteToExecuteWhenFinished =
			(Action)Delegate.CreateDelegate(typeof(Action), methodExecuteToExecuteWhenFinished);

		public static Queue<QueuedLongEvent2> eventQueue = new Queue<QueuedLongEvent2>();

		public static bool initCopyComplete = false;

		private static QueuedLongEvent2 currentEvent = null;
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

		public static void CopyEventQueue()
		{
			while (eventQueue1.Count > 0)
			{
				object obj = eventQueue1.Dequeue();
				FieldInfo action = obj.GetType().GetField("eventAction", BindingFlags.Public | BindingFlags.Instance);
				FieldInfo textKeyField = obj.GetType().GetField("eventTextKey", BindingFlags.Public | BindingFlags.Instance);
				FieldInfo doAsynchronouslyField = obj.GetType().GetField("doAsynchronously", BindingFlags.Public | BindingFlags.Instance);
				FieldInfo exceptionHandlerField = obj.GetType().GetField("exceptionHandler", BindingFlags.Public | BindingFlags.Instance);
				FieldInfo canEverUseStandardWindowField = obj.GetType().GetField("canEverUseStandardWindow", BindingFlags.Public | BindingFlags.Instance);
				FieldInfo showExtraUIInfoField = obj.GetType().GetField("showExtraUIInfo", BindingFlags.Public | BindingFlags.Instance);

				QueuedLongEvent2 queuedLongEvent = new QueuedLongEvent2
				{
					eventAction = (Action)(action.GetValue(obj)),
					eventTextKey = (string)textKeyField.GetValue(obj),
					doAsynchronously = (bool)doAsynchronouslyField.GetValue(obj),
					exceptionHandler = (Action<Exception>)exceptionHandlerField.GetValue(obj),
					canEverUseStandardWindow = (bool)canEverUseStandardWindowField.GetValue(obj),
					showExtraUIInfo = (bool)showExtraUIInfoField.GetValue(obj)
				};
				eventQueue.Enqueue(queuedLongEvent);
			}
			initCopyComplete = true;
		}

		public static bool ExecuteToExecuteWhenFinished()
		{
			if (executingToExecuteWhenFinished)
			{
				//Log.Warning("Already executing.");
				//return false;
			}

			executingToExecuteWhenFinished = true;
			if (toExecuteWhenFinished2.Count > 0)
			{
				DeepProfiler.Start("ExecuteToExecuteWhenFinished()");
			}
			Action action;
			//for (int i = 0; i < toExecuteWhenFinished.Count; i++)
			while (toExecuteWhenFinished2.TryDequeue(out action))
			{
				/*
				try
				{
					action = toExecuteWhenFinished[i];
				}
				catch (ArgumentOutOfRangeException)
				{
					break;
				}
				*/
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

			//
			toExecuteWhenFinished.Clear();
			executingToExecuteWhenFinished = false;
			return false;
		}
		public static PropertyInfo propertyShouldWaitUntilDisplayed;
		public static MethodInfo getterShouldWaitUntilDisplayed;
		public static bool ExecuteWhenFinished(Action action)
		{
			toExecuteWhenFinished2.Enqueue(action);
			return true;
		}
		public static bool DrawLongEventWindowContents(Rect rect)
		{
			if (currentEvent == null)
			{
				return false;
			}

			if (Event.current.type == EventType.Repaint)
			{
				currentEvent.alreadyDisplayed = true;
			}

			Text.Font = GameFont.Small;
			Text.Anchor = TextAnchor.MiddleCenter;
			float num = 0f;
			if (levelLoadOp != null)
			{
				float f = 1f;
				if (!levelLoadOp.isDone)
				{
					f = levelLoadOp.progress;
				}

				TaggedString taggedString = "LoadingAssets".Translate() + " " + f.ToStringPercent();
				num = Text.CalcSize(taggedString).x;
				Widgets.Label(rect, taggedString);
			}
			else
			{
				lock (CurrentEventTextLock)
				{
					num = Text.CalcSize(currentEvent.eventText).x;
					Widgets.Label(rect, currentEvent.eventText);
				}
			}

			Text.Anchor = TextAnchor.MiddleLeft;
			rect.xMin = rect.center.x + num / 2f;
			Widgets.Label(rect, (!currentEvent.UseAnimatedDots) ? "..." : GenText.MarchingEllipsis());
			Text.Anchor = TextAnchor.UpperLeft;
			return false;
		}
		public static bool get_AnyEventNowOrWaiting(ref bool __result)
		{
			if (!initCopyComplete) return true;
			if (currentEvent == null)
			{
				__result = eventQueue.Count > 0;
			}
			return false;
		}
		public static bool get_AnyEventWhichDoesntUseStandardWindowNowOrWaiting(ref bool __result)
		{
			if (!initCopyComplete) return true;
			QueuedLongEvent2 queuedLongEvent = currentEvent;
			if (queuedLongEvent != null && !queuedLongEvent.UseStandardWindow)
			{
				__result = true;
				return false;
			}
			__result = eventQueue.Any((QueuedLongEvent2 x) => !x.UseStandardWindow);
			return false;
		}
		public static bool get_ShouldWaitForEvent(ref bool __result)
		{
			if (!initCopyComplete) return true;
			if (!LongEventHandler.AnyEventNowOrWaiting)
			{
				__result = false;
				return false;
			}

			if (currentEvent != null && !currentEvent.UseStandardWindow)
			{
				__result = true;
				return false;
			}

			if (Find.UIRoot == null || Find.WindowStack == null)
			{
				__result = true;
				return false;
			}
			__result = false;
			return false;
		}
		public static bool LongEventsOnGUI()
		{
			if (currentEvent == null)
			{
				GameplayTipWindow.ResetTipTimer();
				return false;
			}

			float num = StatusRectSize.x;
			lock (CurrentEventTextLock)
			{
				Text.Font = GameFont.Small;
				num = Mathf.Max(num, Text.CalcSize(currentEvent.eventText + "...").x + 40f);
			}

			bool flag = Find.UIRoot != null && !currentEvent.UseStandardWindow && currentEvent.showExtraUIInfo;
			bool flag2 = Find.UIRoot != null && Current.Game != null && !currentEvent.UseStandardWindow && currentEvent.showExtraUIInfo;
			Vector2 vector = flag2 ? ModSummaryWindow.GetEffectiveSize() : Vector2.zero;
			float num2 = StatusRectSize.y;
			if (flag2)
			{
				num2 += 17f + vector.y;
			}

			if (flag)
			{
				num2 += 17f + GameplayTipWindow.WindowSize.y;
			}

			float num3 = ((float)UI.screenHeight - num2) / 2f;
			Vector2 offset = new Vector2(((float)UI.screenWidth - GameplayTipWindow.WindowSize.x) / 2f, num3 + StatusRectSize.y + 17f);
			Vector2 offset2 = new Vector2(((float)UI.screenWidth - vector.x) / 2f, offset.y + GameplayTipWindow.WindowSize.y + 17f);
			Rect r = new Rect(((float)UI.screenWidth - num) / 2f, num3, num, StatusRectSize.y);
			r = r.Rounded();
			if (!currentEvent.UseStandardWindow || Find.UIRoot == null || Find.WindowStack == null)
			{
				if (UIMenuBackgroundManager.background == null)
				{
					UIMenuBackgroundManager.background = new UI_BackgroundMain();
				}

				UIMenuBackgroundManager.background.BackgroundOnGUI();
				Widgets.DrawShadowAround(r);
				Widgets.DrawWindowBackground(r);
				actionDrawLongEventWindowContents(r);
				if (flag)
				{
					GameplayTipWindow.DrawWindow(offset, useWindowStack: false);
				}

				if (flag2)
				{
					ModSummaryWindow.DrawWindow(offset2, useWindowStack: false);
					TooltipHandler.DoTooltipGUI();
				}
			}
			else
			{
				actionDrawLongEventWindow(r);
				if (flag)
				{
					GameplayTipWindow.DrawWindow(offset, useWindowStack: true);
				}
			}
			return false;
		}
		public static bool LongEventsUpdate(ref bool sceneChanged)
		{
			if (!initCopyComplete) return true;

			sceneChanged = false;
			if (currentEvent != null)
			{
				if (currentEvent.eventActionEnumerator != null)
				{
					actionUpdateCurrentEnumeratorEvent();
				}
				else if (currentEvent.doAsynchronously)
				{
					actionUpdateCurrentAsynchronousEvent();
				}
				else
				{
					UpdateCurrentSynchronousEvent(out sceneChanged);
				}
			}

			if (currentEvent == null && eventQueue.Count > 0)
			{
				currentEvent = eventQueue.Dequeue();
				if (currentEvent.eventTextKey == null)
				{
					currentEvent.eventText = "";
				}
				else
				{
					currentEvent.eventText = currentEvent.eventTextKey.Translate();
				}
			}
			return false;
		}
		public static bool UpdateCurrentEnumeratorEvent()
		{
			try
			{
				float num = Time.realtimeSinceStartup + 0.1f;
				do
				{
					if (!currentEvent.eventActionEnumerator.MoveNext())
					{
						(currentEvent.eventActionEnumerator as IDisposable)?.Dispose();
						currentEvent = null;
						eventThread = null;
						levelLoadOp = null;
						ExecuteToExecuteWhenFinished();
						break;
					}
				}
				while (!(num <= Time.realtimeSinceStartup));
			}
			catch (Exception ex)
			{
				Log.Error("Exception from long event: " + ex);
				if (currentEvent != null)
				{
					(currentEvent.eventActionEnumerator as IDisposable)?.Dispose();
					if (currentEvent.exceptionHandler != null)
					{
						currentEvent.exceptionHandler(ex);
					}
				}

				currentEvent = null;
				eventThread = null;
				levelLoadOp = null;
			}
			return false;
		}
		public static bool UpdateCurrentAsynchronousEvent()
		{
			if (eventThread == null)
			{
				eventThread = new Thread((ThreadStart)delegate
				{
					actionRunEventFromAnotherThread(currentEvent.eventAction);
				});
				eventThread.Start();
			}
			else
			{
				if (eventThread.IsAlive)
				{
					return false;
				}

				bool flag = false;
				if (!currentEvent.levelToLoad.NullOrEmpty())
				{
					if (levelLoadOp == null)
					{
						levelLoadOp = SceneManager.LoadSceneAsync(currentEvent.levelToLoad);
					}
					else if (levelLoadOp.isDone)
					{
						flag = true;
					}
				}
				else
				{
					flag = true;
				}

				if (flag)
				{
					currentEvent = null;
					eventThread = null;
					levelLoadOp = null;
					ExecuteToExecuteWhenFinished();
				}
			}
			return false;
		}
		public static bool UpdateCurrentSynchronousEvent(out bool sceneChanged)
		{
			sceneChanged = false;
			if (currentEvent.ShouldWaitUntilDisplayed)
			{
				return false;
			}

			try
			{
				if (currentEvent.eventAction != null)
				{
					currentEvent.eventAction();
				}

				if (!currentEvent.levelToLoad.NullOrEmpty())
				{
					SceneManager.LoadScene(currentEvent.levelToLoad);
					sceneChanged = true;
				}

				currentEvent = null;
				eventThread = null;
				levelLoadOp = null;
				ExecuteToExecuteWhenFinished();
			}
			catch (Exception ex)
			{
				Log.Error("Exception from long event: " + ex);
				if (currentEvent != null && currentEvent.exceptionHandler != null)
				{
					currentEvent.exceptionHandler(ex);
				}

				currentEvent = null;
				eventThread = null;
				levelLoadOp = null;
			}
			return false;
		}
		public static bool RunEventFromAnotherThread(Action action)
		{
			CultureInfoUtility.EnsureEnglish();
			try
			{
				action?.Invoke();
			}
			catch (Exception ex)
			{
				Log.Error("Exception from asynchronous event: " + ex);
				try
				{
					if (currentEvent != null && currentEvent.exceptionHandler != null)
					{
						currentEvent.exceptionHandler(ex);
					}
				}
				catch (Exception arg)
				{
					Log.Error("Exception was thrown while trying to handle exception. Exception: " + arg);
				}
			}
			return false;
		}
		public static bool SetCurrentEventText(string newText)
		{
			lock (CurrentEventTextLock)
			{
				if (currentEvent != null)
				{
					currentEvent.eventText = newText;
				}
			}
			return false;
		}

		public static bool ClearQueuedEvents()
		{
			eventQueue.Clear();
			return false;
		}
		public static bool QueueLongEvent(Action action, string textKey, bool doAsynchronously, Action<Exception> exceptionHandler, bool showExtraUIInfo = true)
		{
			QueuedLongEvent2 queuedLongEvent = new QueuedLongEvent2
			{
				eventAction = action,
				eventTextKey = textKey,
				doAsynchronously = doAsynchronously,
				exceptionHandler = exceptionHandler,
				canEverUseStandardWindow = !LongEventHandler.AnyEventWhichDoesntUseStandardWindowNowOrWaiting,
				showExtraUIInfo = showExtraUIInfo
			};
			eventQueue.Enqueue(queuedLongEvent);
			return false;
		}
		public static bool QueueLongEvent(IEnumerable action, string textKey, Action<Exception> exceptionHandler = null, bool showExtraUIInfo = true)
		{
			QueuedLongEvent2 queuedLongEvent = new QueuedLongEvent2
			{
				eventActionEnumerator = action.GetEnumerator(),
				eventTextKey = textKey,
				doAsynchronously = false,
				exceptionHandler = exceptionHandler,
				canEverUseStandardWindow = !LongEventHandler.AnyEventWhichDoesntUseStandardWindowNowOrWaiting,
				showExtraUIInfo = showExtraUIInfo
			};
			eventQueue.Enqueue(queuedLongEvent);
			return false;
		}
		public static bool QueueLongEvent(Action preLoadLevelAction, string levelToLoad, string textKey, bool doAsynchronously, Action<Exception> exceptionHandler, bool showExtraUIInfo = true)
		{
			QueuedLongEvent2 queuedLongEvent = new QueuedLongEvent2
			{
				eventAction = preLoadLevelAction,
				levelToLoad = levelToLoad,
				eventTextKey = textKey,
				doAsynchronously = doAsynchronously,
				exceptionHandler = exceptionHandler,
				canEverUseStandardWindow = !LongEventHandler.AnyEventWhichDoesntUseStandardWindowNowOrWaiting,
				showExtraUIInfo = showExtraUIInfo
			};
			eventQueue.Enqueue(queuedLongEvent);
			return false;
		}



	}



}

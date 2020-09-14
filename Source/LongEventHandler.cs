using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace RimThreaded
{

    public class LongEventHandler_Patch
	{
		public static bool executingToExecuteWhenFinished =
			AccessTools.StaticFieldRefAccess<bool>(typeof(LongEventHandler), "executingToExecuteWhenFinished");
		public static List<Action> toExecuteWhenFinished =
					AccessTools.StaticFieldRefAccess<List<Action>>(typeof(LongEventHandler), "toExecuteWhenFinished");

		public static bool ExecuteToExecuteWhenFinished()
		{
			if (executingToExecuteWhenFinished)
			{
				Log.Warning("Already executing.", false);
				return false;
			}
			executingToExecuteWhenFinished = true;
			if (toExecuteWhenFinished.Count > 0)
			{
				DeepProfiler.Start("ExecuteToExecuteWhenFinished()");
			}
			Action action;
			for (int i = 0; i < toExecuteWhenFinished.Count; i++)
			{
				action = null;
				try
				{
					action = toExecuteWhenFinished[i];
				} catch (ArgumentOutOfRangeException _) { }
				if (null == action)
					break;
				DeepProfiler.Start(action.Method.DeclaringType.ToString() + " -> " + action.Method.ToString());
				try
				{
					action();
				}
				catch (Exception arg)
				{
					Log.Error("Could not execute post-long-event action. Exception: " + arg, false);
				}
				finally
				{
					DeepProfiler.End();
				}
			}
			if (toExecuteWhenFinished.Count > 0)
			{
				DeepProfiler.End();
			}
			toExecuteWhenFinished.Clear();
			executingToExecuteWhenFinished = false;
			return false;
		}

	}
}

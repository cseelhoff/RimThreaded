using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;
using UnityEngine;
using Verse.AI;
using RimWorld;
using Verse.Sound;
using RimWorld.Planet;

namespace RimThreaded
{
	class RimThreadedMod : Mod
	{
		public static RimThreadedSettings Settings;
		public RimThreadedMod(ModContentPack content) : base(content)
		{
			Settings = GetSettings<RimThreadedSettings>();
		}
		public override void DoSettingsWindowContents(Rect inRect)
		{
			Settings.DoWindowContents(inRect);
			if (Settings.maxThreads != RimThreaded.maxThreads)
			{
				RimThreaded.maxThreads = Math.Max(Settings.maxThreads, 1);
				RimThreaded.RestartAllWorkerThreads();
			}
			RimThreaded.timeoutMS = Math.Max(Settings.timeoutMS, 1);
		}

		public override string SettingsCategory()
		{
			return "RimThreaded";

		}

	}
	
}


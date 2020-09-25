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
	public class RimThreadedSettings : ModSettings
	{
		public int maxThreads = 8;
		public string maxThreadsBuffer = "8";
		public int timeoutMS = 1000;
		public string timeoutMSBuffer = "1000";
		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref maxThreadsBuffer, "maxThreadsBuffer", "8");
			Scribe_Values.Look(ref timeoutMSBuffer, "timeoutMSBuffer", "1000");
		}

		public void DoWindowContents(Rect inRect)
        {
			Listing_Standard listing_Standard = new Listing_Standard();
			listing_Standard.Begin(inRect);
			Widgets.Label(listing_Standard.GetRect(30f), "Total worker threads (recommendation 1-2 per CPU core):");
			Widgets.IntEntry(listing_Standard.GetRect(40f), ref maxThreads, ref maxThreadsBuffer);
			Widgets.Label(listing_Standard.GetRect(30f), "Timeout (in miliseconds) waiting for threads (default: 1000):");
			Widgets.IntEntry(listing_Standard.GetRect(40f), ref timeoutMS, ref timeoutMSBuffer, 10);
			listing_Standard.End();
		}
	}	
}


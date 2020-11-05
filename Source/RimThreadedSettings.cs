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
		public float timeSpeedNormal = 1f;
		public string timeSpeedNormalBuffer = "1";
		public float timeSpeedFast = 3f;
		public string timeSpeedFastBuffer = "3";
		public float timeSpeedSuperfast = 12f;
		public string timeSpeedSuperfastBuffer = "12";
		public float timeSpeedUltrafast = 150f;
		public string timeSpeedUltrafastBuffer = "150";
		public bool suppressTexture2dError = true;
		public string modsText = "";


		public Vector2 scrollPos = new Vector2(0, 0);
		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref maxThreadsBuffer, "maxThreadsBuffer", "8");
			Scribe_Values.Look(ref timeoutMSBuffer, "timeoutMSBuffer", "1000");
			Scribe_Values.Look(ref timeSpeedNormalBuffer, "timeSpeedNormalBuffer", "1");
			Scribe_Values.Look(ref timeSpeedFastBuffer, "timeSpeedFastBuffer", "3");
			Scribe_Values.Look(ref timeSpeedSuperfastBuffer, "timeSpeedSuperfastBuffer", "12");
			Scribe_Values.Look(ref timeSpeedUltrafastBuffer, "timeSpeedUltrafastBuffer", "150");
			Scribe_Values.Look(ref suppressTexture2dError, "suppressTexture2dError", true);
		}

		public void DoWindowContents(Rect inRect)
        {
			Listing_Standard listing_Standard = new Listing_Standard();
			listing_Standard.Begin(inRect);
			Widgets.Label(listing_Standard.GetRect(30f), "Total worker threads (recommendation 1-2 per CPU core):");
			Widgets.IntEntry(listing_Standard.GetRect(40f), ref maxThreads, ref maxThreadsBuffer);
			Widgets.Label(listing_Standard.GetRect(30f), "Timeout (in miliseconds) waiting for threads (default: 1000):");
			Widgets.IntEntry(listing_Standard.GetRect(40f), ref timeoutMS, ref timeoutMSBuffer, 10);
			Widgets.Label(listing_Standard.GetRect(30f), "Timespeed Normal (multiply by 60 for Max TPS):");
			Widgets.TextFieldNumeric<float>(listing_Standard.GetRect(30f), ref timeSpeedNormal, ref timeSpeedNormalBuffer);
			Widgets.Label(listing_Standard.GetRect(30f), "Timespeed Fast (multiply by 60 for Max TPS):");
			Widgets.TextFieldNumeric<float>(listing_Standard.GetRect(30f), ref timeSpeedFast, ref timeSpeedFastBuffer);
			Widgets.Label(listing_Standard.GetRect(30f), "Timespeed Superfast (multiply by 60 for Max TPS):");
			Widgets.TextFieldNumeric<float>(listing_Standard.GetRect(30f), ref timeSpeedSuperfast, ref timeSpeedSuperfastBuffer);
			Widgets.Label(listing_Standard.GetRect(30f), "Timespeed Ultrafast (multiply by 60 for Max TPS):");
			Widgets.TextFieldNumeric<float>(listing_Standard.GetRect(30f), ref timeSpeedUltrafast, ref timeSpeedUltrafastBuffer);
			Widgets.CheckboxLabeled(listing_Standard.GetRect(40f), "Suppress 'Could not load UnityEngine.Texture2D' error:", ref suppressTexture2dError);
			Widgets.TextAreaScrollable(listing_Standard.GetRect(150f), modsText, ref scrollPos);
			listing_Standard.End();
		}
	}	
}


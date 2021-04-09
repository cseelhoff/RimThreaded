using System;
using Verse;
using UnityEngine;

namespace RimThreaded
{
    public class RimThreadedSettings : ModSettings
    {
        public int maxThreads = 8;
        public string maxThreadsBuffer = "8";
        public int timeoutMS = 8000;
        public string timeoutMSBuffer = "8000";
        public float timeSpeedNormal = 1f;
        public string timeSpeedNormalBuffer = "1";
        public float timeSpeedFast = 3f;
        public string timeSpeedFastBuffer = "3";
        public float timeSpeedSuperfast = 12f;
        public string timeSpeedSuperfastBuffer = "12";
        public float timeSpeedUltrafast = 150f;
        public string timeSpeedUltrafastBuffer = "150";
        public bool disablesomealerts = false;
        public bool disablelimits = false;
        public bool disableforcedslowdowns = false;
        public bool showModConflictsAlert = true;
        public float scrollViewHeight;
        public Vector2 scrollPosition;
        public string modsText = "";
        private string Threads;

        public Vector2 scrollPos = new Vector2(0, 0);
        public override void ExposeData()
        {

            try
            {
                Threads = SystemInfo.processorCount.ToString();
            }
            catch (Exception)
            {
                Threads = "8";
            }
            base.ExposeData();
            Scribe_Values.Look(ref maxThreadsBuffer, "maxThreadsBuffer", Threads);
            Scribe_Values.Look(ref timeoutMSBuffer, "timeoutMSBuffer", "8000");
            Scribe_Values.Look(ref timeSpeedNormalBuffer, "timeSpeedNormalBuffer", "1");
            Scribe_Values.Look(ref timeSpeedFastBuffer, "timeSpeedFastBuffer", "3");
            Scribe_Values.Look(ref timeSpeedSuperfastBuffer, "timeSpeedSuperfastBuffer", "12");
            Scribe_Values.Look(ref timeSpeedUltrafastBuffer, "timeSpeedUltrafastBuffer", "150");
            Scribe_Values.Look(ref disablesomealerts, "disablesomealets", false);
            Scribe_Values.Look(ref disablelimits, "disablelimits", false);
            Scribe_Values.Look(ref disableforcedslowdowns, "disableforcedslowdowns", false);
            Scribe_Values.Look(ref showModConflictsAlert, "showModConflictsAlert", true);

        }

        public void DoWindowContents(Rect inRect)
        {
            Listing_Standard listing_Standard = new Listing_Standard();
            Rect viewRect = new Rect(0f, 0f, inRect.width - 16f, scrollViewHeight);
            listing_Standard.BeginScrollView(inRect, ref scrollPosition, ref viewRect);
            Widgets.Label(listing_Standard.GetRect(25f), "Total worker threads (recommendation 1-2 per CPU core):");
            Widgets.IntEntry(listing_Standard.GetRect(37f), ref maxThreads, ref maxThreadsBuffer);
            Widgets.Label(listing_Standard.GetRect(25f), "Timeout (in miliseconds) waiting for threads (default: 8000):");
            Widgets.IntEntry(listing_Standard.GetRect(37f), ref timeoutMS, ref timeoutMSBuffer, 100);
            Widgets.Label(listing_Standard.GetRect(25f), "Timespeed Normal (multiply by 60 for Max TPS):");
            Widgets.TextFieldNumeric(listing_Standard.GetRect(30f), ref timeSpeedNormal, ref timeSpeedNormalBuffer);
            Widgets.Label(listing_Standard.GetRect(25f), "Timespeed Fast (multiply by 60 for Max TPS):");
            Widgets.TextFieldNumeric(listing_Standard.GetRect(30f), ref timeSpeedFast, ref timeSpeedFastBuffer);
            Widgets.Label(listing_Standard.GetRect(25f), "Timespeed Superfast (multiply by 60 for Max TPS):");
            Widgets.TextFieldNumeric(listing_Standard.GetRect(30f), ref timeSpeedSuperfast, ref timeSpeedSuperfastBuffer);
            Widgets.Label(listing_Standard.GetRect(25f), "Timespeed Ultrafast (multiply by 60 for Max TPS):");
            Widgets.TextFieldNumeric(listing_Standard.GetRect(30f), ref timeSpeedUltrafast, ref timeSpeedUltrafastBuffer);
            Widgets.CheckboxLabeled(listing_Standard.GetRect(27f), "Disable alert updates at 4x speed:", ref disablesomealerts);
            Widgets.CheckboxLabeled(listing_Standard.GetRect(27f), "Disable thread/worker limit (debugging):", ref disablelimits);
            Widgets.CheckboxLabeled(listing_Standard.GetRect(27f), "Disable slowdown on combat:", ref disableforcedslowdowns);
            Widgets.CheckboxLabeled(listing_Standard.GetRect(27f), "Show alert when conflicting mods are detected:", ref showModConflictsAlert);
            Widgets.TextAreaScrollable(listing_Standard.GetRect(300f), modsText, ref scrollPos);
            listing_Standard.EndScrollView(ref viewRect);
            scrollViewHeight = viewRect.height;
        }
    }
}


using RimWorld;
using System;
using System.Text.RegularExpressions;
using static Verse.Translator;

namespace RimThreaded
{
    class ColoredText_Patch
    {
        [ThreadStatic] public static Regex DaysRegex;
        [ThreadStatic] public static Regex HoursRegex;
        [ThreadStatic] public static Regex SecondsRegex;
        [ThreadStatic] public static Regex ColonistCountRegex;

        public static void InitializeThreadStatics()
        {
            DaysRegex = new Regex(string.Format((string)"PeriodDays".Translate(), (object)"\\d+\\.?\\d*"));
            HoursRegex = new Regex(string.Format((string)"PeriodHours".Translate(), (object)"\\d+\\.?\\d*"));
            SecondsRegex = new Regex(string.Format((string)"PeriodSeconds".Translate(), (object)"\\d+\\.?\\d*"));
            ColonistCountRegex = new Regex("\\d+\\.?\\d* " + ("(" + FactionDefOf.PlayerColony.pawnsPlural + "|" + FactionDefOf.PlayerColony.pawnSingular + ")"));
        }

    }
}

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

    public class SeasonUtility_Patch
    {
        public static Dictionary<int, Dictionary<float, Season>> yearLatitudeSeason = new Dictionary<int, Dictionary<float, Season>>();
        public static bool GetReportedSeason(ref Season __result, float yearPct, float latitude)
        {
            int year1000 = (int)(yearPct * 1000f);
            if (!yearLatitudeSeason.TryGetValue(year1000, out Dictionary<float, Season> latitudeSeason))
            {
                latitudeSeason = new Dictionary<float, Season>();
                yearLatitudeSeason.Add(year1000, latitudeSeason);
            }
            if(!latitudeSeason.TryGetValue(latitude, out Season season)) {
                SeasonUtility.GetSeason(yearPct, latitude, out float spring, out float summer, out float fall, out float winter, out float permanentSummer, out float permanentWinter);
                if (permanentSummer == 1f)
                {
                    season = Season.PermanentSummer;
                }

                if (permanentWinter == 1f)
                {
                    season = Season.PermanentWinter;
                }
                season = GenMath.MaxBy(Season.Spring, spring, Season.Summer, summer, Season.Fall, fall, Season.Winter, winter);
                latitudeSeason.Add(latitude, season);
            }
            __result = season;
            return false;
        }



    }
}

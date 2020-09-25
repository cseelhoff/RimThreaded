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

    public class DateNotifier_Patch
	{
        public static bool FindPlayerHomeWithMinTimezone(DateNotifier __instance, ref Map __result)
        {
            List<Map> maps = Find.Maps;
            Map map = null;
            int num = -1;
            for (int i = 0; i < maps.Count; i++)
            {
                if (maps[i].IsPlayerHome)
                {
                    if (maps.Count > 1)
                    {
                        int num2 = GenDate.TimeZoneAt(Find.WorldGrid.LongLatOf(maps[i].Tile).x);
                        if (map == null || num2 < num)
                        {
                            map = maps[i];
                            num = num2;
                        }
                    } else
                    {
                        map = maps[i];
                    }
                }
            }

            __result = map;
            return false;
        }



    }
}

using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RimThreaded
{

    public class DateNotifier_Patch
    {
        internal static void RunDestructivePatches()
        {
            Type original = typeof(DateNotifier);
            Type patched = typeof(DateNotifier_Patch);
            RimThreadedHarmony.Prefix(original, patched, "FindPlayerHomeWithMinTimezone");
        }

        public static bool FindPlayerHomeWithMinTimezone(DateNotifier __instance, ref Map __result)
        {
            List<Map> maps = Find.Maps;
            Map map = maps[0];
            int num = -1;
            if (maps.Count > 1)
            {
                for (int i = 0; i < maps.Count; i++)
                {
                    if (maps[i].IsPlayerHome)
                    {
                        int num2 = GenDate.TimeZoneAt(Find.WorldGrid.LongLatOf(maps[i].Tile).x);
                        if (map == null || num2 < num)
                        {
                            map = maps[i];
                            num = num2;
                        }
                    }
                }
            }
            __result = map;
            return false;
        }

    }
}

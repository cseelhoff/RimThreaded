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

    public class ThingCountUtility_Patch
	{
        public static bool AddToList(List<ThingCount> list, Thing thing, int countToAdd)
        {
            ThingCount thingCount;
            for (int i = 0; i < list.Count; i++)
            {
                try
                {
                    thingCount = list[i];
                } catch (ArgumentOutOfRangeException) { break; }
                if (thingCount.Thing == thing)
                {
                    thingCount = thingCount.WithCount(thingCount.Count + countToAdd);
                    return false;
                }
            }
            lock (list)
            {
                list.Add(new ThingCount(thing, countToAdd));
            }
            return false;
        }

    }
}

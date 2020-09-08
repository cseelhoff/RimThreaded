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
    public class FireUtility_Patch
    {
        public static bool ContainsStaticFire(ref bool __result, IntVec3 c, Map map)
        {
            Thing[] arrayThingList;
            List<Thing> thingList = map.thingGrid.ThingsListAt(c);
            lock (thingList)
            {
                arrayThingList = thingList.ToArray();
            }
            for (int index = 0; index < arrayThingList.Length; ++index)
            {
                if (arrayThingList[index] is Fire fire && fire.parent == null)
                {
                    __result = true;
                    return false;
                }
            }
            __result = false;
            return false;
        }
    }
}

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

    public class WaterSplash_Patch {

		public static bool Impact(WaterSplash __instance, Thing hitThing)
		{
			Projectile_Patch.Base_Impact(__instance, hitThing);
			List<Thing> list = new List<Thing>();
			foreach (Thing item in __instance.Map.thingGrid.ThingsAt(__instance.Position))
			{
				if (item.def == ThingDefOf.Fire)
				{
					list.Add(item);
				}
			}

			foreach (Thing item2 in list)
			{
				item2.Destroy();
			}
			return false;
		}
	}
}

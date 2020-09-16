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

    public class Spark_Patch {

		public static bool Impact(Spark __instance, Thing hitThing)
		{
			Map map = __instance.Map;
			Projectile_Patch.Base_Impact(__instance, hitThing);
			FireUtility.TryStartFireIn(__instance.Position, map, 0.1f);
			return false;
		}
	}
}

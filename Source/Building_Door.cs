using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using System.Reflection;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{

    public class Building_Door_Patch
    {
		public static FieldRef<CompPowerTrader, bool> powerOnInt =
            FieldRefAccess<CompPowerTrader, bool>("powerOnInt");
		public static FieldRef<Building_Door, bool> openInt =
            FieldRefAccess<Building_Door, bool>("openInt");
		public static FieldRef<Building_Door, bool> holdOpenInt =
            FieldRefAccess<Building_Door, bool>("holdOpenInt");
		public static FieldRef<Building_Door, int> ticksUntilClose =
            FieldRefAccess<Building_Door, int>("ticksUntilClose");
		public static PropertyInfo canTryCloseAutomatically = DeclaredProperty(typeof(Building_Door), "CanTryCloseAutomatically");

		public static FieldRef<CompPowerTrader, bool> powerOnIntFieldRef = FieldRefAccess<CompPowerTrader, bool>("powerOnInt");

		public static bool get_DoorPowerOn(Building_Door __instance, ref bool __result)
		{
			CompPowerTrader pc = __instance.powerComp;
			bool poweron = false;
			if (pc != null)
			{
				try
				{
					//poweron = pc.PowerOn;
					poweron = powerOnIntFieldRef(pc);
				}
				catch (NullReferenceException) { }
			}
			__result = poweron;
			return false;
		}
        public static bool get_BlockedOpenMomentary(Building_Door __instance, ref bool __result)
        { 
            List<Thing> thingList = __instance.Position.GetThingList(__instance.Map); 
            for (int i = 0; i < thingList.Count; i++)
            {
                Thing thing = thingList[i];
                if (thing?.def?.category == ThingCategory.Item || thing?.def?.category == ThingCategory.Pawn)
                {
                    __result = true;
                } else
                {
                    __result = false;
				}
            }
            return false;
        }

		internal static void RunDestructivePatches()
        {
			Type original = typeof(Building_Door);
			Type patched = typeof(Building_Door_Patch);
			RimThreadedHarmony.Prefix(original, patched, "get_DoorPowerOn");
            RimThreadedHarmony.Prefix(original, patched, "get_BlockedOpenMomentary");
        }
	}
}

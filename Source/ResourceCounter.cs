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

    public class ResourceCounter_Patch
	{
		public static AccessTools.FieldRef<ResourceCounter, Dictionary<ThingDef, int>> countedAmounts =
			AccessTools.FieldRefAccess<ResourceCounter, Dictionary<ThingDef, int>>("countedAmounts");
		public static bool get_TotalHumanEdibleNutrition(ResourceCounter __instance, ref float __result)
		{
            float num = 0f;
            ThingDef[] tdArray;
            lock (countedAmounts(__instance))
            {
                tdArray = countedAmounts(__instance).Keys.ToArray();
            }
            for (int i = 0; i < tdArray.Length; i++)
            {
                ThingDef td = tdArray[i];
                int value = 0;
                if (td.IsNutritionGivingIngestible && td.ingestible.HumanEdible)
                {
                    try
                    {
                        value = countedAmounts(__instance)[td];
                    }
                    catch { continue; }
                    num += td.GetStatValueAbstract(StatDefOf.Nutrition) * (float)value;
                }
            }

            __result = num;
            return false;
        }

	}
}

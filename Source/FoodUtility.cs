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

    public class FoodUtility_Patch
	{
        public static SimpleCurve FoodOptimalityEffectFromMoodCurve =
            AccessTools.StaticFieldRefAccess<SimpleCurve>(typeof(FoodUtility), "FoodOptimalityEffectFromMoodCurve");
        public static bool FoodOptimality(ref float __result,
              Pawn eater,
              Thing foodSource,
              ThingDef foodDef,
              float dist,
              bool takingToInventory = false)
        {
            float num = 300f - dist;
            switch (foodDef.ingestible.preferability)
            {
                case FoodPreferability.NeverForNutrition:
                    __result = - 9999999f;
                    return false;
                case FoodPreferability.DesperateOnly:
                    num -= 150f;
                    break;
                case FoodPreferability.DesperateOnlyForHumanlikes:
                    if (eater.RaceProps.Humanlike)
                    {
                        num -= 150f;
                        break;
                    }
                    break;
            }
            CompRottable comp = foodSource.TryGetComp<CompRottable>();
            if (comp != null)
            {
                if (comp.Stage == RotStage.Dessicated)
                {
                    __result = -9999999f;
                    return false;
                }
                if (!takingToInventory && comp.Stage == RotStage.Fresh && comp.TicksUntilRotAtCurrentTemp < 30000)
                    num += 12f;
            }
            if (eater.needs != null && eater.needs.mood != null)
            {
                List<ThoughtDef> thoughtDefList = FoodUtility.ThoughtsFromIngesting(eater, foodSource, foodDef);
                for (int index = 0; index < thoughtDefList.Count; ++index)
                {
                    ThoughtDef thoughtDef = thoughtDefList[index];
                    if (null != thoughtDef)
                    {
                        num += FoodOptimalityEffectFromMoodCurve.Evaluate(thoughtDef.stages[0].baseMoodEffect);
                    }
                }
            }
            if (foodDef.ingestible != null)
            {
                if (eater.RaceProps.Humanlike)
                    num += foodDef.ingestible.optimalityOffsetHumanlikes;
                else if (eater.RaceProps.Animal)
                    num += foodDef.ingestible.optimalityOffsetFeedingAnimals;
            }
            __result = num;
            return false;
        }

    }
}

using RimWorld;
using System;
using Verse;

namespace RimThreaded
{
    class FoodUtility_Patch
    {
        internal static void RunDestructivePatches()
        {
            Type original = typeof(FoodUtility);
            Type patched = typeof(FoodUtility_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(GetMeatSourceCategory));
        }

        public static bool GetMeatSourceCategory(ref MeatSourceCategory __result, ThingDef source)
        {
            IngestibleProperties ingestible = source.ingestible;
            if (ingestible == null)
            {
                __result = MeatSourceCategory.Undefined;
                return false;
            }
            if ((ingestible.foodType & FoodTypeFlags.Meat) != FoodTypeFlags.Meat)
            {
                __result = MeatSourceCategory.NotMeat;
                return false;
            }
            if (ingestible.sourceDef != null && ingestible.sourceDef.race != null && ingestible.sourceDef.race.Humanlike)
            {
                __result = MeatSourceCategory.Humanlike;
                return false;
            }
            __result = ingestible.sourceDef != null && ingestible.sourceDef.race.FleshType != null && ingestible.sourceDef.race.FleshType == FleshTypeDefOf.Insectoid ? MeatSourceCategory.Insect : MeatSourceCategory.Undefined;
            return false;
        }
    }
}

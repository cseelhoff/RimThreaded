using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace RimThreaded
{
    class FoodUtility_Patch
    {
        [ThreadStatic] public static float? bestFoodSourceOnMap_minNutrition_NewTemp;
        [ThreadStatic] public static HashSet<Thing> filtered;
        [ThreadStatic] public static List<Pawn> tmpPredatorCandidates;
        [ThreadStatic] public static List<ThoughtDef> ingestThoughts;

        public static void InitializeThreadStatics()
        {
            filtered = new HashSet<Thing>();
            tmpPredatorCandidates = new List<Pawn>();
            ingestThoughts = new List<ThoughtDef>();
            bestFoodSourceOnMap_minNutrition_NewTemp = null;
        }
        public static void RunNonDestructivePatches()
        {
            Type original = typeof(FoodUtility);
            Type patched = typeof(FoodUtility_Patch);
            RimThreadedHarmony.AddAllMatchingFields(original, patched);
            RimThreadedHarmony.TranspileFieldReplacements(original, "BestFoodSourceOnMap");
            RimThreadedHarmony.TranspileFieldReplacements(original, "BestPawnToHuntForPredator");
            RimThreadedHarmony.TranspileFieldReplacements(original, "ThoughtsFromIngesting");
            RimThreadedHarmony.TranspileFieldReplacements(original, "AddIngestThoughtsFromIngredient");
            RimThreadedHarmony.TranspileFieldReplacements(typeof(WorkGiver_InteractAnimal), "TakeFoodForAnimalInteractJob");
        }
    }
}
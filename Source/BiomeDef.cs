using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using System;

namespace RimThreaded
{

    public class BiomeDef_Patch
    {
        public static AccessTools.FieldRef<BiomeDef, Dictionary<ThingDef, float>> cachedPlantCommonalities =
            AccessTools.FieldRefAccess<BiomeDef, Dictionary<ThingDef, float>>("cachedPlantCommonalities");
        public static AccessTools.FieldRef<BiomeDef, List<BiomePlantRecord>> wildPlants =
            AccessTools.FieldRefAccess<BiomeDef, List<BiomePlantRecord>>("wildPlants");
        public static AccessTools.FieldRef<BiomeDef, float> cachedPlantCommonalitiesSum =
            AccessTools.FieldRefAccess<BiomeDef, float>("cachedPlantCommonalitiesSum");
        //public static AccessTools.FieldRef<BiomeDef, float?> cachedLowestWildPlantOrder =
        //AccessTools.FieldRefAccess<BiomeDef, float?>("cachedLowestWildPlantOrder");


        internal static void RunDestructivePatches()
        {
            Type original = typeof(BiomeDef);
            Type patched = typeof(BiomeDef_Patch);
            RimThreadedHarmony.Prefix(original, patched, "CachePlantCommonalitiesIfShould");
        }
        public static bool CachePlantCommonalitiesIfShould(BiomeDef __instance)
        {
            if (cachedPlantCommonalities(__instance) != null)
            {
                return false;
            }
            if (cachedPlantCommonalities(__instance) != null)
            {
                return false;
            }
            lock (__instance) //TODO more efficient lock
            {
                Dictionary<ThingDef, float> localCachedPlantCommonalities = new Dictionary<ThingDef, float>();
                for (int i = 0; i < wildPlants(__instance).Count; i++)
                {
                    BiomePlantRecord wildPlant = wildPlants(__instance)[i];
                    ThingDef plant = wildPlant.plant;
                    if (plant != null)
                    {
                        localCachedPlantCommonalities[plant] = wildPlant.commonality;
                    }
                }

                foreach (ThingDef allDef in DefDatabase<ThingDef>.AllDefs)
                {
                    if (allDef.plant != null && allDef.plant.wildBiomes != null)
                    {
                        for (int j = 0; j < allDef.plant.wildBiomes.Count; j++)
                        {
                            if (allDef.plant.wildBiomes[j].biome == __instance)
                            {
                                localCachedPlantCommonalities.Add(allDef, allDef.plant.wildBiomes[j].commonality);
                            }
                        }
                    }
                }
                cachedPlantCommonalitiesSum(__instance) = localCachedPlantCommonalities.Sum((KeyValuePair<ThingDef, float> x) => x.Value);
                cachedPlantCommonalities(__instance) = localCachedPlantCommonalities;

            }
            return false;
        }

    }
}

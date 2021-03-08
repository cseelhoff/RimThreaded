using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using UnityEngine;

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
        public static AccessTools.FieldRef<BiomeDef, float?> cachedLowestWildPlantOrder =
            AccessTools.FieldRefAccess<BiomeDef, float?>("cachedLowestWildPlantOrder");

        public static bool get_LowestWildAndCavePlantOrder(BiomeDef __instance, ref float __result)
        {
            if (!cachedLowestWildPlantOrder(__instance).HasValue)
            {
                cachedLowestWildPlantOrder(__instance) = 2.14748365E+09f;
                List<ThingDef> allWildPlants = __instance.AllWildPlants;
                for (int i = 0; i < allWildPlants.Count; i++)
                {
                    ThingDef wildPlant = allWildPlants[i];
                    if (null != wildPlant)
                    {
                        cachedLowestWildPlantOrder(__instance) = Mathf.Min(cachedLowestWildPlantOrder(__instance).Value, wildPlant.plant.wildOrder);
                    }
                }

                List<ThingDef> allDefsListForReading = DefDatabase<ThingDef>.AllDefsListForReading;
                for (int j = 0; j < allDefsListForReading.Count; j++)
                {
                    if (allDefsListForReading[j].category == ThingCategory.Plant && allDefsListForReading[j].plant.cavePlant)
                    {
                        cachedLowestWildPlantOrder(__instance) = Mathf.Min(cachedLowestWildPlantOrder(__instance).Value, allDefsListForReading[j].plant.wildOrder);
                    }
                }
            }

            __result = cachedLowestWildPlantOrder(__instance).Value;
            return false;
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

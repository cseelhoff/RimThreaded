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

    public class BiomeDef_Patch
    {
        public static AccessTools.FieldRef<BiomeDef, Dictionary<ThingDef, float>> cachedPlantCommonalities =
            AccessTools.FieldRefAccess<BiomeDef, Dictionary<ThingDef, float>>("cachedPlantCommonalities");
        public static AccessTools.FieldRef<BiomeDef, List<BiomePlantRecord>> wildPlants =
            AccessTools.FieldRefAccess<BiomeDef, List<BiomePlantRecord>>("wildPlants");
        public static AccessTools.FieldRef<BiomeDef, float> cachedPlantCommonalitiesSum =
            AccessTools.FieldRefAccess<BiomeDef, float>("cachedPlantCommonalitiesSum");


        public static bool CachePlantCommonalitiesIfShould(BiomeDef __instance)
        {
            if (cachedPlantCommonalities(__instance) != null)
            {
                return false;
            }

            cachedPlantCommonalities(__instance) = new Dictionary<ThingDef, float>();
            for (int i = 0; i < wildPlants(__instance).Count; i++)
            {
                if (wildPlants(__instance)[i].plant != null)
                {
                        cachedPlantCommonalities(__instance)[wildPlants(__instance)[i].plant] = wildPlants(__instance)[i].commonality;
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
                            cachedPlantCommonalities(__instance).Add(allDef, allDef.plant.wildBiomes[j].commonality);
                        }
                    }
                }
            }
            cachedPlantCommonalitiesSum(__instance) = cachedPlantCommonalities(__instance).Sum((KeyValuePair<ThingDef, float> x) => x.Value);
            return false;
        }



    }
}

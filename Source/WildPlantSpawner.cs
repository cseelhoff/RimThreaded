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

    public class WildPlantSpawner_Patch
    {
        public static AccessTools.FieldRef<WildPlantSpawner, Map> map =
            AccessTools.FieldRefAccess<WildPlantSpawner, Map>("map");
        public static AccessTools.FieldRef<WildPlantSpawner, bool> hasWholeMapNumDesiredPlantsCalculated =
            AccessTools.FieldRefAccess<WildPlantSpawner, bool>("hasWholeMapNumDesiredPlantsCalculated");
        public static AccessTools.FieldRef<WildPlantSpawner, float> calculatedWholeMapNumDesiredPlants =
            AccessTools.FieldRefAccess<WildPlantSpawner, float>("calculatedWholeMapNumDesiredPlants");
        public static AccessTools.FieldRef<WildPlantSpawner, float> calculatedWholeMapNumDesiredPlantsTmp =
            AccessTools.FieldRefAccess<WildPlantSpawner, float>("calculatedWholeMapNumDesiredPlantsTmp");
        public static AccessTools.FieldRef<WildPlantSpawner, int> calculatedWholeMapNumNonZeroFertilityCells =
            AccessTools.FieldRefAccess<WildPlantSpawner, int>("calculatedWholeMapNumNonZeroFertilityCells");
        public static AccessTools.FieldRef<WildPlantSpawner, int> calculatedWholeMapNumNonZeroFertilityCellsTmp =
            AccessTools.FieldRefAccess<WildPlantSpawner, int>("calculatedWholeMapNumNonZeroFertilityCellsTmp");
        public static AccessTools.FieldRef<WildPlantSpawner, int> cycleIndex =
            AccessTools.FieldRefAccess<WildPlantSpawner, int>("cycleIndex");

        public static List<ThingDef> allCavePlants =
            AccessTools.StaticFieldRefAccess<List<ThingDef>>(typeof(WildPlantSpawner), "allCavePlants");
        public static SimpleCurve GlobalPctSelectionWeightBias =
            AccessTools.StaticFieldRefAccess<SimpleCurve>(typeof(WildPlantSpawner), "GlobalPctSelectionWeightBias");

        public static bool WildPlantSpawnerTickInternal(WildPlantSpawner __instance)
        {
            Map map2 = map(__instance);
            int area = map2.Area;
            int num = Mathf.CeilToInt((float)area * 0.0001f);
            float currentPlantDensity = __instance.CurrentPlantDensity;
            if (!hasWholeMapNumDesiredPlantsCalculated(__instance))
            {
                calculatedWholeMapNumDesiredPlants(__instance) = __instance.CurrentWholeMapNumDesiredPlants;
                calculatedWholeMapNumNonZeroFertilityCells(__instance) = __instance.CurrentWholeMapNumNonZeroFertilityCells;
                hasWholeMapNumDesiredPlantsCalculated(__instance) = true;
            }
            int num2 = Mathf.CeilToInt(10000f);
            float chance = calculatedWholeMapNumDesiredPlants(__instance) / (float)calculatedWholeMapNumNonZeroFertilityCells(__instance);
            map2.cellsInRandomOrder.Get(0); //Create List If Should
            RimThreaded.WildPlantSpawnerCycleIndexOffset = num + cycleIndex(__instance);
            RimThreaded.WildPlantSpawnerArea = area;
            RimThreaded.WildPlantSpawnerCellsInRandomOrder = map2.cellsInRandomOrder;
            RimThreaded.WildPlantSpawnerMap = map2;
            RimThreaded.WildPlantSpawnerCurrentPlantDensity = currentPlantDensity;
            RimThreaded.DesiredPlants = calculatedWholeMapNumDesiredPlants(__instance);
            RimThreaded.DesiredPlantsTmp1000 = 1000 * (int)calculatedWholeMapNumDesiredPlantsTmp(__instance);
            RimThreaded.FertilityCellsTmp = calculatedWholeMapNumNonZeroFertilityCellsTmp(__instance);
            RimThreaded.DesiredPlants2Tmp1000 = 0;
            RimThreaded.FertilityCells2Tmp = 0;
            RimThreaded.WildPlantSpawnerInstance = __instance;
            RimThreaded.WildPlantSpawnerChance = chance;
            RimThreaded.WildPlantSpawnerTicks = num;
            cycleIndex(__instance) = (cycleIndex(__instance) + num) % area;
            return false;
        }
        public static bool GoodRoofForCavePlant2(Map map2, IntVec3 c)
        {
            return c.GetRoof(map2)?.isNatural ?? false;
        }
        public static bool CanRegrowAt2(Map map2, IntVec3 c)
        {
            if (c.GetTemperature(map2) > 0f)
            {
                if (c.Roofed(map2))
                {
                    return GoodRoofForCavePlant2(map2, c);
                }

                return true;
            }

            return false;
        }
        public static float GetDesiredPlantsCountAt2(Map map2, IntVec3 c, IntVec3 forCell, float plantDensity)
        {
            return Mathf.Min(GetBaseDesiredPlantsCountAt2(map2, c) * plantDensity * forCell.GetTerrain(map2).fertility, 1f);
        }
        public static float GetBaseDesiredPlantsCountAt2(Map map2, IntVec3 c)
        {
            float num = c.GetTerrain(map2).fertility;
            if (GoodRoofForCavePlant2(map2, c))
            {
                num *= 0.5f;
            }

            return num;
        }
        public static Dictionary<ThingDef, float> CalculateDistancesToNearbyClusters2(WildPlantSpawner __instance, IntVec3 c)
        {
            Map map2 = map(__instance);
            //nearbyClusters.Clear();
            //nearbyClustersList.Clear();
            Dictionary<ThingDef, List<float>> nearbyClusters = new Dictionary<ThingDef, List<float>>();
            List<KeyValuePair<ThingDef, List<float>>> nearbyClustersList = new List<KeyValuePair<ThingDef, List<float>>>();

            int num = GenRadial.NumCellsInRadius(map2.Biome.MaxWildAndCavePlantsClusterRadius * 2);
            for (int i = 0; i < num; i++)
            {
                IntVec3 intVec = c + GenRadial.RadialPattern[i];
                if (!intVec.InBounds(map2))
                {
                    continue;
                }

                List<Thing> list = map2.thingGrid.ThingsListAtFast(intVec);
                for (int j = 0; j < list.Count; j++)
                {
                    Thing thing = list[j];
                    if (thing.def.category == ThingCategory.Plant && thing.def.plant.GrowsInClusters)
                    {
                        float item = intVec.DistanceToSquared(c);
                        if (!nearbyClusters.TryGetValue(thing.def, out List<float> value))
                        {
                            value = new List<float>(); //SimplePool<List<float>>.Get();
                            nearbyClusters.Add(thing.def, value);
                            nearbyClustersList.Add(new KeyValuePair<ThingDef, List<float>>(thing.def, value));
                        }

                        value.Add(item);
                    }
                }
            }

            //distanceSqToNearbyClusters.Clear();
            Dictionary<ThingDef, float> distanceSqToNearbyClusters = new Dictionary<ThingDef, float>();
            for (int k = 0; k < nearbyClustersList.Count; k++)
            {
                List<float> value2 = nearbyClustersList[k].Value;
                value2.Sort();
                distanceSqToNearbyClusters.Add(nearbyClustersList[k].Key, value2[value2.Count / 2]);
                value2.Clear();
                SimplePool<List<float>>.Return(value2);
            }
            return distanceSqToNearbyClusters;
        }
        public static bool CheckSpawnWildPlantAt(WildPlantSpawner __instance, ref bool __result, IntVec3 c, float plantDensity, float wholeMapNumDesiredPlants, bool setRandomGrowth = false)
        {
            Map map2 = map(__instance);
            if (plantDensity <= 0f || c.GetPlant(map2) != null || c.GetCover(map2) != null || c.GetEdifice(map2) != null || map2.fertilityGrid.FertilityAt(c) <= 0f || !PlantUtility.SnowAllowsPlanting(c, map2))
            {
                __result = false;
                return false;
            }

            bool cavePlants = GoodRoofForCavePlant2(map2, c);
            if (SaturatedAt2(map2, c, plantDensity, cavePlants, wholeMapNumDesiredPlants))
            {
                __result = false;
                return false;
            }
            List<ThingDef> tmpPossiblePlants = new List<ThingDef>();
            CalculatePlantsWhichCanGrowAt2(__instance, c, tmpPossiblePlants, cavePlants, plantDensity);
            if (!tmpPossiblePlants.Any())
            {
                __result = false;
                return false;
            }

            Dictionary<ThingDef, float> distanceSqToNearbyClusters = CalculateDistancesToNearbyClusters2(__instance, c);
            List<KeyValuePair<ThingDef, float>> tmpPossiblePlantsWithWeight = new List<KeyValuePair<ThingDef, float>>();
            tmpPossiblePlantsWithWeight.Clear();
            for (int i = 0; i < tmpPossiblePlants.Count; i++)
            {
                float value = PlantChoiceWeight2(__instance, tmpPossiblePlants[i], c, distanceSqToNearbyClusters, wholeMapNumDesiredPlants, plantDensity);
                tmpPossiblePlantsWithWeight.Add(new KeyValuePair<ThingDef, float>(tmpPossiblePlants[i], value));
            }

            if (!tmpPossiblePlantsWithWeight.TryRandomElementByWeight((KeyValuePair<ThingDef, float> x) => x.Value, out KeyValuePair<ThingDef, float> result))
            {
                __result = false;
                return false;
            }

            Plant plant = (Plant)ThingMaker.MakeThing(result.Key);
            if (setRandomGrowth)
            {
                plant.Growth = Rand.Range(0.07f, 1f);
                if (plant.def.plant.LimitedLifespan)
                {
                    plant.Age = Rand.Range(0, Mathf.Max(plant.def.plant.LifespanTicks - 50, 0));
                }
            }

            GenSpawn.Spawn(plant, c, map2);
            __result = true;
            return false;
        }
        private static float PlantChoiceWeight2(WildPlantSpawner __instance, ThingDef plantDef, IntVec3 c, Dictionary<ThingDef, float> distanceSqToNearbyClusters, float wholeMapNumDesiredPlants, float plantDensity)
        {
            float commonalityOfPlant = GetCommonalityOfPlant2(map(__instance), plantDef);
            float commonalityPctOfPlant = GetCommonalityPctOfPlant2(__instance, plantDef);
            float num = commonalityOfPlant;
            if (num <= 0f)
            {
                return num;
            }

            float num2 = 0.5f;
            if ((float)map(__instance).listerThings.ThingsInGroup(ThingRequestGroup.Plant).Count > wholeMapNumDesiredPlants / 2f && !plantDef.plant.cavePlant)
            {
                num2 = (float)map(__instance).listerThings.ThingsOfDef(plantDef).Count / (float)map(__instance).listerThings.ThingsInGroup(ThingRequestGroup.Plant).Count / commonalityPctOfPlant;
                num *= GlobalPctSelectionWeightBias.Evaluate(num2);
            }

            if (plantDef.plant.GrowsInClusters && num2 < 1.1f)
            {
                float num3 = plantDef.plant.cavePlant ? __instance.CavePlantsCommonalitiesSum : map(__instance).Biome.PlantCommonalitiesSum;
                float x = commonalityOfPlant * plantDef.plant.wildClusterWeight / (num3 - commonalityOfPlant + commonalityOfPlant * plantDef.plant.wildClusterWeight);
                float outTo = 1f / ((float)Math.PI * (float)plantDef.plant.wildClusterRadius * (float)plantDef.plant.wildClusterRadius);
                outTo = GenMath.LerpDoubleClamped(commonalityPctOfPlant, 1f, 1f, outTo, x);
                if (distanceSqToNearbyClusters.TryGetValue(plantDef, out float value))
                {
                    float x2 = Mathf.Sqrt(value);
                    num *= GenMath.LerpDoubleClamped((float)plantDef.plant.wildClusterRadius * 0.9f, (float)plantDef.plant.wildClusterRadius * 1.1f, plantDef.plant.wildClusterWeight, outTo, x2);
                }
                else
                {
                    num *= outTo;
                }
            }

            if (plantDef.plant.wildEqualLocalDistribution)
            {
                float f = wholeMapNumDesiredPlants * commonalityPctOfPlant;
                float a = (float)Mathf.Max(map(__instance).Size.x, map(__instance).Size.z) / Mathf.Sqrt(f) * 2f;
                if (plantDef.plant.GrowsInClusters)
                {
                    a = Mathf.Max(a, (float)plantDef.plant.wildClusterRadius * 1.6f);
                }

                a = Mathf.Max(a, 7f);
                if (a <= 25f)
                {
                    num *= LocalPlantProportionsWeightFactor2(__instance, c, commonalityPctOfPlant, plantDensity, a, plantDef);
                }
            }

            return num;
        }
        private static float LocalPlantProportionsWeightFactor2(WildPlantSpawner __instance, IntVec3 c, float commonalityPct, float plantDensity, float radiusToScan, ThingDef plantDef)
        {
            float numDesiredPlantsLocally = 0f;
            int numPlants = 0;
            int numPlantsThisDef = 0;
            RegionTraverser.BreadthFirstTraverse(c, map(__instance), (Region from, Region to) => c.InHorDistOf(to.extentsClose.ClosestCellTo(c), radiusToScan), delegate (Region reg)
            {
                numDesiredPlantsLocally += GetDesiredPlantsCountIn2(map(__instance), reg, c, plantDensity);
                numPlants += reg.ListerThings.ThingsInGroup(ThingRequestGroup.Plant).Count;
                numPlantsThisDef += reg.ListerThings.ThingsOfDef(plantDef).Count;
                return false;
            });
            if (numDesiredPlantsLocally * commonalityPct < 2f)
            {
                return 1f;
            }

            if ((float)numPlants <= numDesiredPlantsLocally * 0.5f)
            {
                return 1f;
            }

            float t = (float)numPlantsThisDef / (float)numPlants / commonalityPct;
            return Mathf.Lerp(7f, 1f, t);
        }


        private static float GetCommonalityOfPlant2(Map map2, ThingDef plant)
        {
            if (!plant.plant.cavePlant)
            {
                return map2.Biome.CommonalityOfPlant(plant);
            }

            return plant.plant.cavePlantWeight;
        }


        private static bool SaturatedAt2(Map map2, IntVec3 c, float plantDensity, bool cavePlants, float wholeMapNumDesiredPlants)
        {
            int num = GenRadial.NumCellsInRadius(20f);
            if (wholeMapNumDesiredPlants * ((float)num / (float)map2.Area) <= 4f || !map2.Biome.wildPlantsCareAboutLocalFertility)
            {
                return (float)map2.listerThings.ThingsInGroup(ThingRequestGroup.Plant).Count >= wholeMapNumDesiredPlants;
            }

            float numDesiredPlantsLocally = 0f;
            int numPlants = 0;
            RegionTraverser.BreadthFirstTraverse(c, map2, (Region from, Region to) => c.InHorDistOf(to.extentsClose.ClosestCellTo(c), 20f), delegate (Region reg)
            {
                numDesiredPlantsLocally += GetDesiredPlantsCountIn2(map2, reg, c, plantDensity);
                numPlants += reg.ListerThings.ThingsInGroup(ThingRequestGroup.Plant).Count;
                return false;
            });
            return (float)numPlants >= numDesiredPlantsLocally;
        }
        public static float GetDesiredPlantsCountIn2(Map map2, Region reg, IntVec3 forCell, float plantDensity)
        {
            return Mathf.Min(reg.GetBaseDesiredPlantsCount() * plantDensity * forCell.GetTerrain(map2).fertility, reg.CellCount);
        }
        private static void CalculatePlantsWhichCanGrowAt2(WildPlantSpawner __instance, IntVec3 c, List<ThingDef> outPlants, bool cavePlants, float plantDensity)
        {
            outPlants.Clear();
            if (cavePlants)
            {
                for (int i = 0; i < allCavePlants.Count; i++)
                {
                    if (allCavePlants[i].CanEverPlantAt_NewTemp(c, map(__instance)))
                    {
                        outPlants.Add(allCavePlants[i]);
                    }
                }

                return;
            }

            List<ThingDef> allWildPlants = map(__instance).Biome.AllWildPlants;
            for (int j = 0; j < allWildPlants.Count; j++)
            {
                ThingDef thingDef = allWildPlants[j];
                if (!thingDef.CanEverPlantAt_NewTemp(c, map(__instance)))
                {
                    continue;
                }

                if (thingDef.plant.wildOrder != map(__instance).Biome.LowestWildAndCavePlantOrder)
                {
                    float num = 7f;
                    if (thingDef.plant.GrowsInClusters)
                    {
                        num = Math.Max(num, (float)thingDef.plant.wildClusterRadius * 1.5f);
                    }

                    if (!EnoughLowerOrderPlantsNearby2(__instance, c, plantDensity, num, thingDef))
                    {
                        continue;
                    }
                }

                outPlants.Add(thingDef);
            }
        }
        private static bool EnoughLowerOrderPlantsNearby2(WildPlantSpawner __instance, IntVec3 c, float plantDensity, float radiusToScan, ThingDef plantDef)
        {
            float num = 0f;
            //tmpPlantDefsLowerOrder.Clear();
            List<ThingDef> tmpPlantDefsLowerOrder = new List<ThingDef>();
            List<ThingDef> allWildPlants = map(__instance).Biome.AllWildPlants;
            for (int i = 0; i < allWildPlants.Count; i++)
            {
                ThingDef wildPlant = allWildPlants[i];
                if (null != wildPlant)
                {
                    if (wildPlant.plant.wildOrder < plantDef.plant.wildOrder)
                    {
                        num += GetCommonalityPctOfPlant2(__instance, wildPlant);
                        tmpPlantDefsLowerOrder.Add(wildPlant);
                    }
                }
            }

            float numDesiredPlantsLocally = 0f;
            int numPlantsLowerOrder = 0;
            RegionTraverser.BreadthFirstTraverse(c, map(__instance), (Region from, Region to) => c.InHorDistOf(to.extentsClose.ClosestCellTo(c), radiusToScan), delegate (Region reg)
            {
                numDesiredPlantsLocally += GetDesiredPlantsCountIn2(map(__instance), reg, c, plantDensity);
                for (int j = 0; j < tmpPlantDefsLowerOrder.Count; j++)
                {
                    numPlantsLowerOrder += reg.ListerThings.ThingsOfDef(tmpPlantDefsLowerOrder[j]).Count;
                }

                return false;
            });
            float num2 = numDesiredPlantsLocally * num;
            if (num2 < 4f)
            {
                return true;
            }

            return (float)numPlantsLowerOrder / num2 >= 0.57f;
        }

        private static float GetCommonalityPctOfPlant2(WildPlantSpawner __instance, ThingDef plant)
        {
            if (!plant.plant.cavePlant)
            {
                return map(__instance).Biome.CommonalityPctOfPlant(plant);
            }

            return GetCommonalityOfPlant2(__instance, plant) / __instance.CavePlantsCommonalitiesSum;
        }
        private static float GetCommonalityOfPlant2(WildPlantSpawner __instance, ThingDef plant)
        {
            if (!plant.plant.cavePlant)
            {
                return map(__instance).Biome.CommonalityOfPlant(plant);
            }

            return plant.plant.cavePlantWeight;
        }






    }
}

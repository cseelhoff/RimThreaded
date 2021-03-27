using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using UnityEngine;
using System.Threading;
using System.Reflection;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{

    public class WildPlantSpawner_Patch
    {
        [ThreadStatic]
        static Dictionary<ThingDef, List<float>> nearbyClusters;

        [ThreadStatic]
        static List<KeyValuePair<ThingDef, List<float>>> nearbyClustersList;

        [ThreadStatic]
        static Dictionary<ThingDef, float> distanceSqToNearbyClusters;

        [ThreadStatic]
        static List<ThingDef> tmpPlantDefsLowerOrder;

        [ThreadStatic]
        static List<KeyValuePair<ThingDef, float>> tmpPossiblePlantsWithWeight;

        [ThreadStatic]
        static List<ThingDef> tmpPossiblePlants;

        public static FieldRef<WildPlantSpawner, Map> map = FieldRefAccess<WildPlantSpawner, Map>("map");
        public static FieldRef<WildPlantSpawner, bool> hasWholeMapNumDesiredPlantsCalculated =
            FieldRefAccess<WildPlantSpawner, bool>("hasWholeMapNumDesiredPlantsCalculated");
        public static FieldRef<WildPlantSpawner, float> calculatedWholeMapNumDesiredPlants =
            FieldRefAccess<WildPlantSpawner, float>("calculatedWholeMapNumDesiredPlants");
        public static FieldRef<WildPlantSpawner, float> calculatedWholeMapNumDesiredPlantsTmp =
            FieldRefAccess<WildPlantSpawner, float>("calculatedWholeMapNumDesiredPlantsTmp");
        public static FieldRef<WildPlantSpawner, int> calculatedWholeMapNumNonZeroFertilityCells =
            FieldRefAccess<WildPlantSpawner, int>("calculatedWholeMapNumNonZeroFertilityCells");
        public static FieldRef<WildPlantSpawner, int> calculatedWholeMapNumNonZeroFertilityCellsTmp =
            FieldRefAccess<WildPlantSpawner, int>("calculatedWholeMapNumNonZeroFertilityCellsTmp");
        public static FieldRef<WildPlantSpawner, int> cycleIndex =
            FieldRefAccess<WildPlantSpawner, int>("cycleIndex");

        public static List<ThingDef> allCavePlants =
            StaticFieldRefAccess<List<ThingDef>>(typeof(WildPlantSpawner), "allCavePlants");
        public static SimpleCurve GlobalPctSelectionWeightBias =
            StaticFieldRefAccess<SimpleCurve>(typeof(WildPlantSpawner), "GlobalPctSelectionWeightBias");

        private static readonly MethodInfo methodGoodRoofForCavePlant =
            Method(typeof(WildPlantSpawner), "GoodRoofForCavePlant", new Type[] { typeof(IntVec3) });
        public static readonly Func<WildPlantSpawner, IntVec3, bool> funcGoodRoofForCavePlant =
            (Func<WildPlantSpawner, IntVec3, bool>)Delegate.CreateDelegate(typeof(Func<WildPlantSpawner, IntVec3, bool>), methodGoodRoofForCavePlant);

        private static readonly MethodInfo methodGetDesiredPlantsCountIn =
            Method(typeof(WildPlantSpawner), "GetDesiredPlantsCountIn", new Type[] { typeof(Region), typeof(IntVec3), typeof(float) });
        private static readonly Func<WildPlantSpawner, Region, IntVec3, float, float> funcGetDesiredPlantsCountIn =
            (Func<WildPlantSpawner, Region, IntVec3, float, float>)Delegate.CreateDelegate(typeof(Func<WildPlantSpawner, Region, IntVec3, float, float>), methodGetDesiredPlantsCountIn);

        private static readonly MethodInfo methodGetCommonalityPctOfPlant =
            Method(typeof(WildPlantSpawner), "GetCommonalityPctOfPlant", new Type[] { typeof(ThingDef) });
        private static readonly Func<WildPlantSpawner, ThingDef, float> funcGetCommonalityPctOfPlant =
            (Func<WildPlantSpawner, ThingDef, float>)Delegate.CreateDelegate(typeof(Func<WildPlantSpawner, ThingDef, float>), methodGetCommonalityPctOfPlant);

        private static readonly MethodInfo methodPlantChoiceWeight =
            Method(typeof(WildPlantSpawner), "PlantChoiceWeight", new Type[] { typeof(ThingDef), typeof(IntVec3), typeof(Dictionary<ThingDef, float>), typeof(float), typeof(float) });
        private static readonly Func<WildPlantSpawner, ThingDef, IntVec3, Dictionary<ThingDef, float>, float, float, float> funcPlantChoiceWeight =
            (Func<WildPlantSpawner, ThingDef, IntVec3, Dictionary<ThingDef, float>, float, float, float>)Delegate.CreateDelegate(typeof(Func<WildPlantSpawner, ThingDef, IntVec3, Dictionary<ThingDef, float>, float, float, float>), methodPlantChoiceWeight);

        private static readonly MethodInfo methodSaturatedAt =
            Method(typeof(WildPlantSpawner), "SaturatedAt", new Type[] { typeof(IntVec3), typeof(float), typeof(bool), typeof(float) });
        private static readonly Func<WildPlantSpawner, IntVec3, float, bool, float, bool> funcSaturatedAt =
            (Func<WildPlantSpawner, IntVec3, float, bool, float, bool>)Delegate.CreateDelegate(typeof(Func<WildPlantSpawner, IntVec3, float, bool, float, bool>), methodSaturatedAt);

        private static readonly MethodInfo methodCalculatePlantsWhichCanGrowAt =
            Method(typeof(WildPlantSpawner), "CalculatePlantsWhichCanGrowAt", new Type[] { typeof(IntVec3), typeof(List<ThingDef>), typeof(bool), typeof(float) });
        private static readonly Action<WildPlantSpawner, IntVec3, List<ThingDef>, bool, float> actionCalculatePlantsWhichCanGrowAt =
            (Action<WildPlantSpawner, IntVec3, List<ThingDef>, bool, float>)Delegate.CreateDelegate(typeof(Action<WildPlantSpawner, IntVec3, List<ThingDef>, bool, float>), methodCalculatePlantsWhichCanGrowAt);



        public static bool WildPlantSpawnerTickInternal(WildPlantSpawner __instance)
        {
            Map map2 = map(__instance);
            int area = map2.Area;
            int num = Mathf.CeilToInt(area * 0.0001f);
            float currentPlantDensity = __instance.CurrentPlantDensity;
            if (!hasWholeMapNumDesiredPlantsCalculated(__instance))
            {
                calculatedWholeMapNumDesiredPlants(__instance) = __instance.CurrentWholeMapNumDesiredPlants;
                calculatedWholeMapNumNonZeroFertilityCells(__instance) = __instance.CurrentWholeMapNumNonZeroFertilityCells;
                hasWholeMapNumDesiredPlantsCalculated(__instance) = true;
            }
            //int num2 = Mathf.CeilToInt(10000f);
            float chance = calculatedWholeMapNumDesiredPlants(__instance) / calculatedWholeMapNumNonZeroFertilityCells(__instance);
            map2.cellsInRandomOrder.Get(0); //This helps call "Create List If Should"
            int index = Interlocked.Increment(ref RimThreaded.wildPlantSpawnerCount) - 1;
            int newNum = Interlocked.Add(ref RimThreaded.wildPlantSpawnerTicksCount, num);
            RimThreaded.wildPlantSpawners[index].WildPlantSpawnerTicks = newNum;
            RimThreaded.wildPlantSpawners[index].WildPlantSpawnerCycleIndexOffset = num + cycleIndex(__instance);
            RimThreaded.wildPlantSpawners[index].WildPlantSpawnerArea = area;
            RimThreaded.wildPlantSpawners[index].WildPlantSpawnerCellsInRandomOrder = map2.cellsInRandomOrder;
            RimThreaded.wildPlantSpawners[index].WildPlantSpawnerMap = map2;
            RimThreaded.wildPlantSpawners[index].WildPlantSpawnerCurrentPlantDensity = currentPlantDensity;
            RimThreaded.wildPlantSpawners[index].DesiredPlants = calculatedWholeMapNumDesiredPlants(__instance);
            RimThreaded.wildPlantSpawners[index].DesiredPlantsTmp1000 = 1000 * (int)calculatedWholeMapNumDesiredPlantsTmp(__instance);
            RimThreaded.wildPlantSpawners[index].FertilityCellsTmp = calculatedWholeMapNumNonZeroFertilityCellsTmp(__instance);
            RimThreaded.wildPlantSpawners[index].DesiredPlants2Tmp1000 = 0;
            RimThreaded.wildPlantSpawners[index].FertilityCells2Tmp = 0;
            RimThreaded.wildPlantSpawners[index].WildPlantSpawnerInstance = __instance;
            RimThreaded.wildPlantSpawners[index].WildPlantSpawnerChance = chance;
            cycleIndex(__instance) = (cycleIndex(__instance) + num) % area;
            return false;
        }


        private static Dictionary<ThingDef, float> CalculateDistancesToNearbyClusters2(WildPlantSpawner __instance, IntVec3 c)
        {
            Map map2 = map(__instance);
            if (nearbyClusters == null)
            {
                nearbyClusters = new Dictionary<ThingDef, List<float>>();
            } else
            {
                nearbyClusters.Clear();
            }
            if (nearbyClustersList == null)
            {
                nearbyClustersList = new List<KeyValuePair<ThingDef, List<float>>>();
            }
            else
            {
                nearbyClustersList = new List<KeyValuePair<ThingDef, List<float>>>();
            }

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
                            value = new List<float>();
                            nearbyClusters.Add(thing.def, value);
                            nearbyClustersList.Add(new KeyValuePair<ThingDef, List<float>>(thing.def, value));
                        }

                        value.Add(item);
                    }
                }
            }

            if (distanceSqToNearbyClusters == null)
            {
                distanceSqToNearbyClusters = new Dictionary<ThingDef, float>();
            }
            else
            {
                distanceSqToNearbyClusters.Clear();
            }
            for (int k = 0; k < nearbyClustersList.Count; k++)
            {
                List<float> value2 = nearbyClustersList[k].Value;
                value2.Sort();
                distanceSqToNearbyClusters.Add(nearbyClustersList[k].Key, value2[value2.Count / 2]);
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

            bool cavePlants = funcGoodRoofForCavePlant(__instance, c);
            if (funcSaturatedAt(__instance, c, plantDensity, cavePlants, wholeMapNumDesiredPlants))
            {
                __result = false;
                return false;
            }
            if (tmpPossiblePlants == null)
            {
                tmpPossiblePlants = new List<ThingDef>();
            } else
            {
                tmpPossiblePlants.Clear();
            }
            actionCalculatePlantsWhichCanGrowAt(__instance, c, tmpPossiblePlants, cavePlants, plantDensity);
            if (!tmpPossiblePlants.Any())
            {
                __result = false;
                return false;
            }

            Dictionary<ThingDef, float> distanceSqToNearbyClusters = CalculateDistancesToNearbyClusters2(__instance, c);
            if (tmpPossiblePlantsWithWeight == null)
            {
                tmpPossiblePlantsWithWeight = new List<KeyValuePair<ThingDef, float>>();
            }
            else
            {
                tmpPossiblePlantsWithWeight.Clear();
            }
            for (int i = 0; i < tmpPossiblePlants.Count; i++)
            {
                float value = funcPlantChoiceWeight(__instance, tmpPossiblePlants[i], c, distanceSqToNearbyClusters, wholeMapNumDesiredPlants, plantDensity);
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

        public static bool EnoughLowerOrderPlantsNearby(WildPlantSpawner __instance, IntVec3 c, float plantDensity, float radiusToScan, ThingDef plantDef)
        {
            float num = 0f;
            if (tmpPlantDefsLowerOrder == null)
            {
                tmpPlantDefsLowerOrder = new List<ThingDef>();
            } else
            {
                tmpPlantDefsLowerOrder.Clear();
            }
            List<ThingDef> allWildPlants = map(__instance).Biome.AllWildPlants;
            for (int i = 0; i < allWildPlants.Count; i++)
            {
                ThingDef wildPlant = allWildPlants[i];
                if (null != wildPlant)
                {
                    if (wildPlant.plant.wildOrder < plantDef.plant.wildOrder)
                    {
                        num += funcGetCommonalityPctOfPlant(__instance, wildPlant);
                        tmpPlantDefsLowerOrder.Add(wildPlant);
                    }
                }
            }

            float numDesiredPlantsLocally = 0f;
            int numPlantsLowerOrder = 0;
            RegionTraverser.BreadthFirstTraverse(c, map(__instance), (Region from, Region to) => c.InHorDistOf(to.extentsClose.ClosestCellTo(c), radiusToScan), delegate (Region reg)
            {
                numDesiredPlantsLocally += funcGetDesiredPlantsCountIn(__instance, reg, c, plantDensity);
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

            return numPlantsLowerOrder / num2 >= 0.57f;
        }

        internal static void RunDestructivePatches()
        {
            Type original = typeof(WildPlantSpawner);
            Type patched = typeof(WildPlantSpawner_Patch);
            RimThreadedHarmony.Prefix(original, patched, "CheckSpawnWildPlantAt");
            RimThreadedHarmony.Prefix(original, patched, "WildPlantSpawnerTickInternal");
        }
    }
}

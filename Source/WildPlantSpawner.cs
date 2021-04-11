using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;
using System.Threading;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{

    public class WildPlantSpawner_Patch
    {
        [ThreadStatic] public static Dictionary<ThingDef, List<float>> nearbyClusters;
        [ThreadStatic] public static List<KeyValuePair<ThingDef, List<float>>> nearbyClustersList;
        [ThreadStatic] public static Dictionary<ThingDef, float> distanceSqToNearbyClusters;
        [ThreadStatic] public static List<ThingDef> tmpPlantDefsLowerOrder;
        [ThreadStatic] public static List<KeyValuePair<ThingDef, float>> tmpPossiblePlantsWithWeight;
        [ThreadStatic] public static List<ThingDef> tmpPossiblePlants;

        public static FieldRef<WildPlantSpawner, Map> map = FieldRefAccess<WildPlantSpawner, Map>("map");

        public static FieldRef<WildPlantSpawner, bool> hasWholeMapNumDesiredPlantsCalculated =
            FieldRefAccess<WildPlantSpawner, bool>("hasWholeMapNumDesiredPlantsCalculated");
        public static FieldRef<WildPlantSpawner, float> calculatedWholeMapNumDesiredPlantsTmp =
            FieldRefAccess<WildPlantSpawner, float>("calculatedWholeMapNumDesiredPlantsTmp");
        public static FieldRef<WildPlantSpawner, float> calculatedWholeMapNumDesiredPlants =
            FieldRefAccess<WildPlantSpawner, float>("calculatedWholeMapNumDesiredPlants");
        public static FieldRef<WildPlantSpawner, int> calculatedWholeMapNumNonZeroFertilityCells =
            FieldRefAccess<WildPlantSpawner, int>("calculatedWholeMapNumNonZeroFertilityCells");
        public static FieldRef<WildPlantSpawner, int> calculatedWholeMapNumNonZeroFertilityCellsTmp =
            FieldRefAccess<WildPlantSpawner, int>("calculatedWholeMapNumNonZeroFertilityCellsTmp");
        public static FieldRef<WildPlantSpawner, int> cycleIndex =
            FieldRefAccess<WildPlantSpawner, int>("cycleIndex");

        internal static void InitializeThreadStatics()
        {
            tmpPossiblePlantsWithWeight = new List<KeyValuePair<ThingDef, float>>();
            tmpPossiblePlants = new List<ThingDef>();
            distanceSqToNearbyClusters = new Dictionary<ThingDef, float>();
            nearbyClusters = new Dictionary<ThingDef, List<float>>();
            nearbyClustersList = new List<KeyValuePair<ThingDef, List<float>>>();
            tmpPlantDefsLowerOrder = new List<ThingDef>();
        }

        static readonly Type original = typeof(WildPlantSpawner);
        static readonly Type patched = typeof(WildPlantSpawner_Patch);

        internal static void RunNonDestructivePatches()
        {
            RimThreadedHarmony.AddAllMatchingFields(original, patched);
            RimThreadedHarmony.TranspileFieldReplacements(original, "CheckSpawnWildPlantAt");
            RimThreadedHarmony.TranspileFieldReplacements(original, "CalculateDistancesToNearbyClusters");
            RimThreadedHarmony.TranspileFieldReplacements(original, "EnoughLowerOrderPlantsNearby");
        }
        internal static void RunDestructivePatches()
        {
            RimThreadedHarmony.Prefix(original, patched, "WildPlantSpawnerTickInternal");
        }

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

    }
}

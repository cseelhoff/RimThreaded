using System;
using System.Collections.Generic;
using System.Reflection;
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
        public static FieldRef<WildPlantSpawner, int> cycleIndexFieldRef =
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
            int index = Interlocked.Increment(ref wildPlantSpawnerCount) - 1;
            int newNum = Interlocked.Add(ref wildPlantSpawnerTicksCount, num);
            wildPlantSpawners[index].WildPlantSpawnerTicks = newNum;
            wildPlantSpawners[index].WildPlantSpawnerCycleIndexOffset = num + cycleIndexFieldRef(__instance);
            wildPlantSpawners[index].WildPlantSpawnerArea = area;
            wildPlantSpawners[index].WildPlantSpawnerCellsInRandomOrder = map2.cellsInRandomOrder;
            wildPlantSpawners[index].WildPlantSpawnerMap = map2;
            wildPlantSpawners[index].WildPlantSpawnerCurrentPlantDensity = currentPlantDensity;
            wildPlantSpawners[index].DesiredPlants = calculatedWholeMapNumDesiredPlants(__instance);
            wildPlantSpawners[index].DesiredPlantsTmp1000 = 1000 * (int)calculatedWholeMapNumDesiredPlantsTmp(__instance);
            wildPlantSpawners[index].FertilityCellsTmp = calculatedWholeMapNumNonZeroFertilityCellsTmp(__instance);
            wildPlantSpawners[index].DesiredPlants2Tmp1000 = 0;
            wildPlantSpawners[index].FertilityCells2Tmp = 0;
            wildPlantSpawners[index].WildPlantSpawnerInstance = __instance;
            wildPlantSpawners[index].WildPlantSpawnerChance = chance;
            cycleIndexFieldRef(__instance) = (cycleIndexFieldRef(__instance) + num) % area;
            return false;
        }

        private static readonly MethodInfo MethodGetDesiredPlantsCountAt =
            Method(typeof(WildPlantSpawner), "GetDesiredPlantsCountAt", new [] { typeof(IntVec3), typeof(IntVec3), typeof(float) });
        private static readonly Func<WildPlantSpawner, IntVec3, IntVec3, float, float> FuncGetDesiredPlantsCountAt =
            (Func<WildPlantSpawner, IntVec3, IntVec3, float, float>)Delegate.CreateDelegate(typeof(Func<WildPlantSpawner, IntVec3, IntVec3, float, float>), MethodGetDesiredPlantsCountAt);
        
        private static readonly MethodInfo MethodCanRegrowAt =
            Method(typeof(WildPlantSpawner), "CanRegrowAt", new [] { typeof(IntVec3) });
        private static readonly Func<WildPlantSpawner, IntVec3, bool> FuncCanRegrowAt =
            (Func<WildPlantSpawner, IntVec3, bool>)Delegate.CreateDelegate(typeof(Func<WildPlantSpawner, IntVec3, bool>), MethodCanRegrowAt);

        private static readonly MethodInfo MethodGoodRoofForCavePlant =
            Method(typeof(WildPlantSpawner), "GoodRoofForCavePlant", new [] { typeof(IntVec3) });
        private static readonly Func<WildPlantSpawner, IntVec3, bool> FuncGoodRoofForCavePlant =
            (Func<WildPlantSpawner, IntVec3, bool>)Delegate.CreateDelegate(typeof(Func<WildPlantSpawner, IntVec3, bool>), MethodGoodRoofForCavePlant);


        public static int wildPlantSpawnerCount = 0; 
        public static int wildPlantSpawnerTicksCompleted = 0;
        public static int wildPlantSpawnerTicksCount = 0;

        public struct WildPlantSpawnerStructure
        {
            public int WildPlantSpawnerTicks;
            public int WildPlantSpawnerCycleIndexOffset;
            public int WildPlantSpawnerArea;
            public Map WildPlantSpawnerMap;
            public MapCellsInRandomOrder WildPlantSpawnerCellsInRandomOrder;
            public float WildPlantSpawnerCurrentPlantDensity;
            public float DesiredPlants;
            public float DesiredPlantsTmp;
            public int DesiredPlants1000;
            public int DesiredPlantsTmp1000;
            public int DesiredPlants2Tmp1000;
            public int FertilityCellsTmp;
            public int FertilityCells2Tmp;
            public int FertilityCells;
            public WildPlantSpawner WildPlantSpawnerInstance;
            public float WildPlantSpawnerChance;
        }
        public static WildPlantSpawnerStructure[] wildPlantSpawners = new WildPlantSpawnerStructure[9999];

        public static void WildPlantSpawnerListTick()
        {
            while (true)
            {
                int ticketIndex = Interlocked.Increment(ref wildPlantSpawnerTicksCompleted) - 1;
                if (ticketIndex >= wildPlantSpawnerTicksCount) return;
                int wildPlantSpawnerIndex = 0;
                WildPlantSpawnerStructure wildPlantSpawner;
                int index;
                while (ticketIndex < wildPlantSpawnerTicksCount)
                {
                    index = ticketIndex;
                    while (ticketIndex >= wildPlantSpawners[wildPlantSpawnerIndex].WildPlantSpawnerTicks)
                    {
                        wildPlantSpawnerIndex++;
                    }

                    if (wildPlantSpawnerIndex > 0)
                        index = ticketIndex - wildPlantSpawners[wildPlantSpawnerIndex - 1].WildPlantSpawnerTicks;
                    try
                    {
                        wildPlantSpawner = wildPlantSpawners[wildPlantSpawnerIndex];
                        int cycleIndex = (wildPlantSpawner.WildPlantSpawnerCycleIndexOffset - index) %
                                         wildPlantSpawner.WildPlantSpawnerArea;
                        IntVec3 intVec = wildPlantSpawner.WildPlantSpawnerCellsInRandomOrder.Get(cycleIndex);

                        if ((wildPlantSpawner.WildPlantSpawnerCycleIndexOffset - index) >
                            wildPlantSpawner.WildPlantSpawnerArea)
                        {
                            Interlocked.Add(ref wildPlantSpawner.DesiredPlants2Tmp1000,
                                1000 * (int) FuncGetDesiredPlantsCountAt(
                                    wildPlantSpawner.WildPlantSpawnerInstance, intVec, intVec,
                                    wildPlantSpawner.WildPlantSpawnerCurrentPlantDensity));
                            if (intVec.GetTerrain(wildPlantSpawners[wildPlantSpawnerIndex].WildPlantSpawnerMap)
                                .fertility > 0f)
                            {
                                Interlocked.Increment(ref wildPlantSpawner.FertilityCells2Tmp);
                            }

                            float mtb = FuncGoodRoofForCavePlant(
                                wildPlantSpawner.WildPlantSpawnerInstance, intVec)
                                ? 130f
                                : wildPlantSpawner.WildPlantSpawnerMap.Biome.wildPlantRegrowDays;
                            if (Rand.Chance(wildPlantSpawner.WildPlantSpawnerChance) &&
                                Rand.MTBEventOccurs(mtb, 60000f, 10000) &&
                                FuncCanRegrowAt(wildPlantSpawner.WildPlantSpawnerInstance, intVec))
                            {
                                wildPlantSpawner.WildPlantSpawnerInstance.CheckSpawnWildPlantAt(intVec,
                                    wildPlantSpawner.WildPlantSpawnerCurrentPlantDensity,
                                    wildPlantSpawner.DesiredPlantsTmp1000 / 1000.0f);
                            }
                        }
                        else
                        {
                            Interlocked.Add(ref wildPlantSpawner.DesiredPlantsTmp1000,
                                1000 * (int) FuncGetDesiredPlantsCountAt(
                                    wildPlantSpawner.WildPlantSpawnerInstance, intVec, intVec,
                                    wildPlantSpawner.WildPlantSpawnerCurrentPlantDensity));
                            if (intVec.GetTerrain(wildPlantSpawner.WildPlantSpawnerMap).fertility > 0f)
                            {
                                Interlocked.Increment(ref wildPlantSpawner.FertilityCellsTmp);
                            }

                            float mtb = FuncGoodRoofForCavePlant(wildPlantSpawner.WildPlantSpawnerInstance, intVec)
                                ? 130f
                                : wildPlantSpawner.WildPlantSpawnerMap.Biome.wildPlantRegrowDays;
                            if (Rand.Chance(wildPlantSpawner.WildPlantSpawnerChance) &&
                                Rand.MTBEventOccurs(mtb, 60000f, 10000) &&
                                FuncCanRegrowAt(wildPlantSpawner.WildPlantSpawnerInstance, intVec))
                            {
                                wildPlantSpawner.WildPlantSpawnerInstance.CheckSpawnWildPlantAt(intVec,
                                    wildPlantSpawner.WildPlantSpawnerCurrentPlantDensity,
                                    wildPlantSpawner.DesiredPlants);
                            }
                        }

                        if (ticketIndex == wildPlantSpawners[wildPlantSpawnerIndex].WildPlantSpawnerTicks - 1)
                        {
                            if ((wildPlantSpawner.WildPlantSpawnerCycleIndexOffset - index) >
                                wildPlantSpawner.WildPlantSpawnerArea)
                            {
                                calculatedWholeMapNumDesiredPlants(wildPlantSpawner.WildPlantSpawnerInstance) =
                                    wildPlantSpawner.DesiredPlantsTmp1000 / 1000.0f;
                                calculatedWholeMapNumDesiredPlantsTmp(wildPlantSpawner.WildPlantSpawnerInstance) =
                                    wildPlantSpawner.DesiredPlants2Tmp1000 / 1000.0f;
                                calculatedWholeMapNumNonZeroFertilityCells(wildPlantSpawner.WildPlantSpawnerInstance) =
                                    wildPlantSpawner.FertilityCellsTmp;
                                calculatedWholeMapNumNonZeroFertilityCellsTmp(wildPlantSpawner
                                    .WildPlantSpawnerInstance) = wildPlantSpawner.FertilityCells2Tmp;
                            }
                            else
                            {
                                calculatedWholeMapNumDesiredPlantsTmp(wildPlantSpawner.WildPlantSpawnerInstance) =
                                    wildPlantSpawner.DesiredPlantsTmp1000 / 1000.0f;
                                calculatedWholeMapNumNonZeroFertilityCells(wildPlantSpawner.WildPlantSpawnerInstance) =
                                    wildPlantSpawner.FertilityCellsTmp;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Exception ticking WildPlantSpawner: " + ex);
                    }
                    ticketIndex = Interlocked.Increment(ref wildPlantSpawnerTicksCompleted) - 1;
                }
            }
        }
    }
}

using System;
using RimWorld;
using Verse;
using UnityEngine;
using System.Threading;

namespace RimThreaded
{

    public class WildPlantSpawner_Patch
    {
        static readonly Type original = typeof(WildPlantSpawner);
        static readonly Type patched = typeof(WildPlantSpawner_Patch);

        internal static void RunDestructivePatches()
        {
            RimThreadedHarmony.Prefix(original, patched, "WildPlantSpawnerTickInternal");
        }

        public static bool WildPlantSpawnerTickInternal(WildPlantSpawner __instance)
        {
            int area = __instance.map.Area;
            int num = Mathf.CeilToInt(area * 0.0001f);
            float currentPlantDensity = __instance.CurrentPlantDensity;
            if (!__instance.hasWholeMapNumDesiredPlantsCalculated)
            {
                __instance.calculatedWholeMapNumDesiredPlants = __instance.CurrentWholeMapNumDesiredPlants;
                __instance.calculatedWholeMapNumNonZeroFertilityCells = __instance.CurrentWholeMapNumNonZeroFertilityCells;
                __instance.hasWholeMapNumDesiredPlantsCalculated = true;
            }
            //int num2 = Mathf.CeilToInt(10000f);
            float chance = __instance.calculatedWholeMapNumDesiredPlants / __instance.calculatedWholeMapNumNonZeroFertilityCells;
            __instance.map.cellsInRandomOrder.Get(0); //This helps call "Create List If Should"
            int index = Interlocked.Increment(ref wildPlantSpawnerCount) - 1;
            int newNum = Interlocked.Add(ref wildPlantSpawnerTicksCount, num);
            wildPlantSpawners[index].ticks = newNum;
            wildPlantSpawners[index].cycleIndexOffset = num + __instance.cycleIndex;
            wildPlantSpawners[index].area = area;
            wildPlantSpawners[index].randomCells = __instance.map.cellsInRandomOrder;
            wildPlantSpawners[index].map = __instance.map;
            wildPlantSpawners[index].plantDensity = currentPlantDensity;
            wildPlantSpawners[index].desiredPlants = __instance.calculatedWholeMapNumDesiredPlants;
            wildPlantSpawners[index].desiredPlantsTmp1000 = 1000 * (int)__instance.calculatedWholeMapNumDesiredPlantsTmp;
            wildPlantSpawners[index].fertilityCellsTmp = __instance.calculatedWholeMapNumNonZeroFertilityCellsTmp;
            wildPlantSpawners[index].desiredPlants2Tmp1000 = 0;
            wildPlantSpawners[index].fertilityCells2Tmp = 0;
            wildPlantSpawners[index].wildPlantSpawnerInstance = __instance;
            wildPlantSpawners[index].chance = chance;
            __instance.cycleIndex = (__instance.cycleIndex + num) % area;
            return false;
        }


        public static int wildPlantSpawnerCount = 0; 
        public static int wildPlantSpawnerTicksCompleted = 0;
        public static int wildPlantSpawnerTicksCount = 0;

        public struct WildPlantSpawnerStructure
        {
            public int ticks;
            public int cycleIndexOffset;
            public int area;
            public Map map;
            public MapCellsInRandomOrder randomCells;
            public float plantDensity;
            public float desiredPlants;
            public int desiredPlantsTmp1000;
            public int desiredPlants2Tmp1000;
            public int fertilityCellsTmp;
            public int fertilityCells2Tmp;
            public int fertilityCells;
            public WildPlantSpawner wildPlantSpawnerInstance;
            public float chance;
        }
        public static WildPlantSpawnerStructure[] wildPlantSpawners = new WildPlantSpawnerStructure[9999];

        public static void WildPlantSpawnerListTick()
        {
            while (true)
            {
                int ticketIndex = Interlocked.Increment(ref wildPlantSpawnerTicksCompleted) - 1;
                if (ticketIndex >= wildPlantSpawnerTicksCount) return;
                int wildPlantSpawnerIndex = 0;
                while (ticketIndex < wildPlantSpawnerTicksCount)
                {
                    int index = ticketIndex;
                    while (ticketIndex >= wildPlantSpawners[wildPlantSpawnerIndex].ticks)
                    {
                        wildPlantSpawnerIndex++;
                    }

                    if (wildPlantSpawnerIndex > 0)
                        index = ticketIndex - wildPlantSpawners[wildPlantSpawnerIndex - 1].ticks;
                    try
                    {
                        WildPlantSpawnerStructure wpsStruct = wildPlantSpawners[wildPlantSpawnerIndex];
                        WildPlantSpawner spawner = wpsStruct.wildPlantSpawnerInstance;
                        int cycleIndex = (wpsStruct.cycleIndexOffset - index) % wpsStruct.area;
                        IntVec3 intVec = wpsStruct.randomCells.Get(cycleIndex);

                        if ((wpsStruct.cycleIndexOffset - index) > wpsStruct.area)
                        {
                            Interlocked.Add(ref wpsStruct.desiredPlants2Tmp1000,
                                1000 * (int)spawner.GetDesiredPlantsCountAt(
                                    intVec, intVec, wpsStruct.plantDensity));
                            if (intVec.GetTerrain(wildPlantSpawners[wildPlantSpawnerIndex].map).fertility > 0f)
                            {
                                Interlocked.Increment(ref wpsStruct.fertilityCells2Tmp);
                            }

                            float mtb = spawner.GoodRoofForCavePlant(intVec) ? 130f : wpsStruct.map.Biome.wildPlantRegrowDays;
                            if (Rand.Chance(wpsStruct.chance) && Rand.MTBEventOccurs(mtb, 60000f, 10000) && spawner.CanRegrowAt(intVec))
                            {
                                spawner.CheckSpawnWildPlantAt(intVec, wpsStruct.plantDensity, wpsStruct.desiredPlantsTmp1000 / 1000.0f);
                            }
                        }
                        else
                        {
                            Interlocked.Add(ref wpsStruct.desiredPlantsTmp1000,
                                1000 * (int)spawner.GetDesiredPlantsCountAt(intVec, intVec, wpsStruct.plantDensity));
                            if (intVec.GetTerrain(wpsStruct.map).fertility > 0f)
                            {
                                Interlocked.Increment(ref wpsStruct.fertilityCellsTmp);
                            }

                            float mtb = spawner.GoodRoofForCavePlant(intVec) ? 130f : wpsStruct.map.Biome.wildPlantRegrowDays;
                            if (Rand.Chance(wpsStruct.chance) && Rand.MTBEventOccurs(mtb, 60000f, 10000) && spawner.CanRegrowAt(intVec))
                            {
                                spawner.CheckSpawnWildPlantAt(intVec, wpsStruct.plantDensity, wpsStruct.desiredPlants);
                            }
                        }

                        if (ticketIndex == wildPlantSpawners[wildPlantSpawnerIndex].ticks - 1)
                        {
                            if ((wpsStruct.cycleIndexOffset - index) >
                                wpsStruct.area)
                            {
                                spawner.calculatedWholeMapNumDesiredPlants = wpsStruct.desiredPlantsTmp1000 / 1000.0f;
                                spawner.calculatedWholeMapNumDesiredPlantsTmp = wpsStruct.desiredPlants2Tmp1000 / 1000.0f;
                                spawner.calculatedWholeMapNumNonZeroFertilityCells = wpsStruct.fertilityCellsTmp;
                                spawner.calculatedWholeMapNumNonZeroFertilityCellsTmp = wpsStruct.fertilityCells2Tmp;
                            }
                            else
                            {
                                spawner.calculatedWholeMapNumDesiredPlantsTmp = wpsStruct.desiredPlantsTmp1000 / 1000.0f;
                                spawner.calculatedWholeMapNumNonZeroFertilityCells = wpsStruct.fertilityCellsTmp;
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

using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{
    class ListerBuildings_Patch
    {

        public static Dictionary<ListerBuildings, List<Building_PlantGrower>> allBuildingsColonistBuilding_PlantGrower =
            new Dictionary<ListerBuildings, List<Building_PlantGrower>>();

        public static List<Building_PlantGrower> get_AllBuildingsColonistBuilding_PlantGrower(ListerBuildings __instance)
        {
            if (!allBuildingsColonistBuilding_PlantGrower.TryGetValue(__instance, out List<Building_PlantGrower> plantGrowers))
            {
                plantGrowers = new List<Building_PlantGrower>();
                for (int i = 0; i < __instance.allBuildingsColonist.Count; i++)
                {
                    if (__instance.allBuildingsColonist[i] is Building_PlantGrower building_PlantGrower)
                    {
                        plantGrowers.Add(building_PlantGrower);
                    }
                }
                allBuildingsColonistBuilding_PlantGrower[__instance] = plantGrowers;
            }
            return plantGrowers;
        }

        public static bool Add(ListerBuildings __instance, Building b)
        {
            if (b.def.building != null && b.def.building.isNaturalRock)
            {
                return false;
            }

            if (b.Faction == Faction.OfPlayer)
            {
                if (b is Building_PlantGrower building_PlantGrower)
                {
                    List<Building_PlantGrower> plantGrowers = get_AllBuildingsColonistBuilding_PlantGrower(__instance);
                    lock (plantGrowers)
                    {
                        plantGrowers.Add(building_PlantGrower);
                    }
                }
                lock (__instance.allBuildingsColonist)
                {
                    __instance.allBuildingsColonist.Add(b);
                }

                if (b is IAttackTarget)
                {
                    lock (__instance.allBuildingsColonistCombatTargets)
                    {
                        __instance.allBuildingsColonistCombatTargets.Add(b);
                    }
                }
            }
            else
            {
                lock (__instance.allBuildingsNonColonist)
                {
                    __instance.allBuildingsNonColonist.Add(b);
                }
            }

            CompProperties_Power compProperties = b.def.GetCompProperties<CompProperties_Power>();
            if (compProperties != null && compProperties.shortCircuitInRain)
            {
                __instance.allBuildingsColonistElecFire.Add(b);
            }
            return false;
        }

        public static bool Remove(ListerBuildings __instance, Building b)
        {
            if (b is Building_PlantGrower building_PlantGrower)
            {
                List<Building_PlantGrower> plantGrowers = get_AllBuildingsColonistBuilding_PlantGrower(__instance);
                lock (plantGrowers)
                {
                    plantGrowers.Remove(building_PlantGrower);
                }
            }
            lock (__instance.allBuildingsColonist)
            {
                __instance.allBuildingsColonist.Remove(b);
            }
            lock (__instance.allBuildingsNonColonist)
            {
                __instance.allBuildingsNonColonist.Remove(b);
            }
            if (b is IAttackTarget)
            {
                lock (__instance.allBuildingsColonistCombatTargets)
                {
                    __instance.allBuildingsColonistCombatTargets.Remove(b);
                }
            }

            CompProperties_Power compProperties = b.def.GetCompProperties<CompProperties_Power>();
            if (compProperties != null && compProperties.shortCircuitInRain)
            {
                lock (__instance.allBuildingsColonistElecFire)
                {
                    __instance.allBuildingsColonistElecFire.Remove(b);
                }
            }
            return false;
        }

    }
}

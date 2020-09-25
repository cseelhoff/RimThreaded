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
using Verse.Noise;

namespace RimThreaded
{

    public class SteadyEnvironmentEffects_Patch
    {
        public static AccessTools.FieldRef<SteadyEnvironmentEffects, Map> map =
            AccessTools.FieldRefAccess<SteadyEnvironmentEffects, Map>("map");
        public static AccessTools.FieldRef<SteadyEnvironmentEffects, float> outdoorMeltAmount =
            AccessTools.FieldRefAccess<SteadyEnvironmentEffects, float>("outdoorMeltAmount");
        public static AccessTools.FieldRef<SteadyEnvironmentEffects, float> snowRate =
            AccessTools.FieldRefAccess<SteadyEnvironmentEffects, float>("snowRate");
        public static AccessTools.FieldRef<SteadyEnvironmentEffects, float> rainRate =
            AccessTools.FieldRefAccess<SteadyEnvironmentEffects, float>("rainRate");
        public static AccessTools.FieldRef<SteadyEnvironmentEffects, float> deteriorationRate =
            AccessTools.FieldRefAccess<SteadyEnvironmentEffects, float>("deteriorationRate");
        public static AccessTools.FieldRef<SteadyEnvironmentEffects, int> cycleIndex =
            AccessTools.FieldRefAccess<SteadyEnvironmentEffects, int>("cycleIndex");
        public static AccessTools.FieldRef<SteadyEnvironmentEffects, ModuleBase> snowNoise =
            AccessTools.FieldRefAccess<SteadyEnvironmentEffects, ModuleBase>("snowNoise");
        private static readonly FloatRange AutoIgnitionTemperatureRange = new FloatRange(240f, 1000f);
        public static bool SteadyEnvironmentEffectsTick(SteadyEnvironmentEffects __instance)
        {
            Map map2 = map(__instance);
            if ((float)Find.TickManager.TicksGame % 97f == 0f && Rand.Chance(0.02f))
            {
                RollForRainFire2(map2);
            }

            outdoorMeltAmount(__instance) = MeltAmountAt2(map2.mapTemperature.OutdoorTemp);
            snowRate(__instance) = map2.weatherManager.SnowRate;
            rainRate(__instance) = map2.weatherManager.RainRate;
            RimThreaded.steadyEnvironmentEffectsCellsInRandomOrder = map2.cellsInRandomOrder;
            deteriorationRate(__instance) = Mathf.Lerp(1f, 5f, rainRate(__instance));
            //int num = Mathf.CeilToInt((float)map2.Area * 0.0006f);
            //int area = map2.Area;
            RimThreaded.steadyEnvironmentEffectsArea = map2.Area;
            RimThreaded.steadyEnvironmentEffectsInstance = __instance;
            int ticks = Mathf.CeilToInt((float)map2.Area * 0.0006f);
            RimThreaded.steadyEnvironmentEffectsCycleIndexOffset = ticks + cycleIndex(__instance);
            RimThreaded.steadyEnvironmentEffectsTicks = ticks;
            cycleIndex(__instance) = (cycleIndex(__instance) + ticks) % map2.Area;
            //RimThreaded.MainThreadWaitLoop();
            /*
            for (int i = 0; i < num; i++)
            {
                if (cycleIndex(__instance) >= area)
                {
                    cycleIndex(__instance) = 0;
                }

                IntVec3 c = map2.cellsInRandomOrder.Get(cycleIndex(__instance));
                //DoCellSteadyEffects(c);
                cycleIndex(__instance)++;
            }
            */
            return false;
        }
        public static void AddFallenSnowAt(SteadyEnvironmentEffects __instance, IntVec3 c, float baseAmount)
        {
            if (snowNoise(__instance) == null)
                snowNoise(__instance) = (ModuleBase)new Perlin(0.0399999991059303, 2.0, 0.5, 5, Rand.Range(0, 651431), Verse.Noise.QualityMode.Medium);
            float num = (snowNoise(__instance).GetValue(c) + 1f) * 0.5f;
            if ((double)num < 0.5)
                num = 0.5f;
            float depthToAdd = baseAmount * num;
            map(__instance).snowGrid.AddDepth(c, depthToAdd);
        }

        private static bool ProtectedByEdifice(IntVec3 c, Map map)
        {
            Building edifice = c.GetEdifice(map);
            return edifice != null && edifice.def.building != null && edifice.def.building.preventDeteriorationOnTop;
        }
        public static void DoCellSteadyEffects(SteadyEnvironmentEffects __instance, IntVec3 c)
        {
            Map map2 = map(__instance);
            Room room = c.GetRoom(map2, RegionType.Set_All);
            bool roofed = map2.roofGrid.Roofed(c);
            bool roomUsesOutdoorTemperature = room != null && room.UsesOutdoorTemperature;
            if (room == null | roomUsesOutdoorTemperature)
            {
                if ((double)outdoorMeltAmount(__instance) > 0.0)
                    map2.snowGrid.AddDepth(c, -outdoorMeltAmount(__instance));
                if (!roofed && (double)snowRate(__instance) > 1.0 / 1000.0)
                    AddFallenSnowAt(__instance, c, 23f / 500f * map2.weatherManager.SnowRate);
            }
            if (room != null)
            {
                bool protectedByEdifice = ProtectedByEdifice(c, map2);
                TerrainDef terrain = c.GetTerrain(map2);
                List<Thing> thingList = c.GetThingList(map2);
                for (int index = 0; index < thingList.Count; ++index)
                {
                    Thing t = thingList[index];
                    if (t is Filth filth)
                    {
                        if (!roofed && t.def.filth.rainWashes && Rand.Chance(rainRate(__instance)))
                            filth.ThinFilth();
                        if (filth.DisappearAfterTicks != 0 && filth.TicksSinceThickened > filth.DisappearAfterTicks)
                            filth.Destroy(DestroyMode.Vanish);
                    }
                    else
                        TryDoDeteriorate(__instance, t, roofed, roomUsesOutdoorTemperature, protectedByEdifice, terrain);
                }
                if (!roomUsesOutdoorTemperature)
                {
                    float temperature = room.Temperature;
                    if ((double)temperature > 0.0)
                    {
                        float num1 = MeltAmountAt(temperature);
                        if ((double)num1 > 0.0)
                            map2.snowGrid.AddDepth(c, -num1);
                        if (room.RegionType.Passable() && (double)temperature > (double)AutoIgnitionTemperatureRange.min)
                        {
                            double num2 = (double)Rand.Value;
                            if (num2 < (double)AutoIgnitionTemperatureRange.InverseLerpThroughRange(temperature) * 0.699999988079071 && Rand.Chance(FireUtility.ChanceToStartFireIn(c, map2)))
                                FireUtility.TryStartFireIn(c, map2, 0.1f);
                            if (num2 < 0.330000013113022)
                                MoteMaker.ThrowHeatGlow(c, map2, 2.3f);
                        }
                    }
                }
            }
            map2.gameConditionManager.DoSteadyEffects(c, map2);
        }
        private static float MeltAmountAt(float temperature)
        {
            if ((double)temperature < 0.0)
                return 0.0f;
            return (double)temperature < 10.0 ? (float)((double)temperature * (double)temperature * 0.00579999992623925 * 0.100000001490116) : temperature * 0.0058f;
        }

        private static void TryDoDeteriorate(SteadyEnvironmentEffects __instance,
          Thing t,
          bool roofed,
          bool roomUsesOutdoorTemperature,
          bool protectedByEdifice,
          TerrainDef terrain)
        {
            if (t is Corpse corpse && corpse.InnerPawn.apparel != null)
            {
                List<Apparel> wornApparel = corpse.InnerPawn.apparel.WornApparel;
                for (int index = 0; index < wornApparel.Count; ++index)
                    TryDoDeteriorate(__instance, (Thing)wornApparel[index], roofed, roomUsesOutdoorTemperature, protectedByEdifice, terrain);
            }
            float num1 = SteadyEnvironmentEffects.FinalDeteriorationRate(t, roofed, roomUsesOutdoorTemperature, protectedByEdifice, terrain, (List<string>)null);
            if ((double)num1 < 1.0 / 1000.0 || !Rand.Chance((float)((double)deteriorationRate(__instance) * (double)num1 / 36.0)))
                return;
            IntVec3 position = t.Position;
            Map map = t.Map;
            int num2 = t.IsInAnyStorage() ? 1 : 0;
            t.TakeDamage(new DamageInfo(DamageDefOf.Deterioration, 1f, 0.0f, -1f, (Thing)null, (BodyPartRecord)null, (ThingDef)null, DamageInfo.SourceCategory.ThingOrUnknown, (Thing)null));
            if (num2 == 0 || !t.Destroyed || !t.def.messageOnDeteriorateInStorage)
                return;
            Messages.Message((string)"MessageDeterioratedAway".Translate((NamedArgument)t.Label), (LookTargets)new TargetInfo(position, map, false), MessageTypeDefOf.NegativeEvent, true);
        }
        private static void RollForRainFire2(Map map2)
        {
            if (!Rand.Chance(0.2f * (float)map2.listerBuildings.allBuildingsColonistElecFire.Count * 
                map2.weatherManager.RainRate))
                return;
            Building building = map2.listerBuildings.allBuildingsColonistElecFire.RandomElement<Building>();
            if (map2.roofGrid.Roofed(building.Position))
                return;
            ShortCircuitUtility.TryShortCircuitInRain((Thing)building);
        }
        private static float MeltAmountAt2(float temperature)
        {
            if ((double)temperature < 0.0)
                return 0.0f;
            return (double)temperature < 10.0 ? (float)((double)temperature * (double)temperature * 0.00579999992623925 * 0.100000001490116) : temperature * 0.0058f;
        }
    }
}

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

    public class WindManager_Patch
    {
        public static AccessTools.FieldRef<WindManager, float> plantSwayHead =
            AccessTools.FieldRefAccess<WindManager, float>("plantSwayHead");
        public static AccessTools.FieldRef<WindManager, float> cachedWindSpeed =
            AccessTools.FieldRefAccess<WindManager, float>("cachedWindSpeed");
        public static AccessTools.FieldRef<WindManager, ModuleBase> windNoise =
            AccessTools.FieldRefAccess<WindManager, ModuleBase>("windNoise");
        public static AccessTools.FieldRef<WindManager, Map> map =
            AccessTools.FieldRefAccess<WindManager, Map>("map");

        private static readonly FloatRange WindSpeedRange =
            AccessTools.StaticFieldRefAccess<FloatRange>(typeof(WindManager), "WindSpeedRange");
        public static List<Material> plantMaterials =
            AccessTools.StaticFieldRefAccess<List<Material>>(typeof(WindManager), "plantMaterials");
        private static float BaseWindSpeedAt2(WindManager __instance, int ticksAbs)
        {
            if (windNoise(__instance) == null)
            {
                int seed = Gen.HashCombineInt(map(__instance).Tile, 122049541) ^ Find.World.info.Seed;
                windNoise(__instance) = new Perlin(3.9999998989515007E-05, 2.0, 0.5, 4, seed, QualityMode.Medium);
                windNoise(__instance) = new ScaleBias(1.5, 0.5, windNoise(__instance));
                windNoise(__instance) = new Clamp(WindSpeedRange.min, WindSpeedRange.max, windNoise(__instance));
            }

            return (float)windNoise(__instance).GetValue(ticksAbs, 0.0, 0.0);
        }

        public static bool WindManagerTick(WindManager __instance)
        {
            cachedWindSpeed(__instance) = BaseWindSpeedAt2(__instance, Find.TickManager.TicksAbs) * map(__instance).weatherManager.CurWindSpeedFactor;
            float curWindSpeedOffset = map(__instance).weatherManager.CurWindSpeedOffset;
            if (curWindSpeedOffset > 0f)
            {
                FloatRange floatRange = WindSpeedRange * map(__instance).weatherManager.CurWindSpeedFactor;
                float num = (cachedWindSpeed(__instance) - floatRange.min) / (floatRange.max - floatRange.min) * (floatRange.max - curWindSpeedOffset);
                cachedWindSpeed(__instance) = curWindSpeedOffset + num;
            }

            List<Thing> list = map(__instance).listerThings.ThingsInGroup(ThingRequestGroup.WindSource);
            for (int i = 0; i < list.Count; i++)
            {
                CompWindSource compWindSource = list[i].TryGetComp<CompWindSource>();
                cachedWindSpeed(__instance) = Mathf.Max(cachedWindSpeed(__instance), compWindSource.wind);
            }

            if (Prefs.PlantWindSway)
            {
                plantSwayHead(__instance) += Mathf.Min(__instance.WindSpeed, 1f);
            }
            else
            {
                plantSwayHead(__instance) = 0f;
            }

            if (Find.CurrentMap == map(__instance))
            {
                TickList_Patch.plantSwayHead = plantSwayHead(__instance);
                TickList_Patch.plantMaterialsCount = plantMaterials.Count;
                TickList_Patch.CreateMonitorThread();
                TickList_Patch.monitorThreadWaitHandle.Set();
            }
            return false;
        }


    }
}

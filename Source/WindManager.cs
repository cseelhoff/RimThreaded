using System;
using System.Collections.Generic;
using Verse;
using UnityEngine;
using Verse.Noise;
using static HarmonyLib.AccessTools;
using System.Reflection;
using System.Threading;

namespace RimThreaded
{

    public class WindManager_Patch
    {
        public static FieldRef<WindManager, float> plantSwayHead = FieldRefAccess<WindManager, float>("plantSwayHead");
        public static FieldRef<WindManager, float> cachedWindSpeed = FieldRefAccess<WindManager, float>("cachedWindSpeed");
        public static FieldRef<WindManager, ModuleBase> windNoise = FieldRefAccess<WindManager, ModuleBase>("windNoise");
        public static FieldRef<WindManager, Map> map = FieldRefAccess<WindManager, Map>("map");

        private static readonly FloatRange WindSpeedRange =
            StaticFieldRefAccess<FloatRange>(typeof(WindManager), "WindSpeedRange");
        public static List<Material> plantMaterials =
            StaticFieldRefAccess<List<Material>>(typeof(WindManager), "plantMaterials");

        private static readonly MethodInfo methodBaseWindSpeedAt =
            Method(typeof(WindManager), "BaseWindSpeedAt", new Type[] { typeof(int) });
        private static readonly Func<WindManager, int, float> funcBaseWindSpeedAt =
            (Func<WindManager, int, float>)Delegate.CreateDelegate(typeof(Func<WindManager, int, float>), methodBaseWindSpeedAt);

        internal static void RunDestructivePatches()
        {
            Type original = typeof(WindManager);
            Type patched = typeof(WindManager_Patch);
            RimThreadedHarmony.Prefix(original, patched, "WindManagerTick");
        }

        public static bool WindManagerTick(WindManager __instance)
        {
            cachedWindSpeed(__instance) = funcBaseWindSpeedAt(__instance, Find.TickManager.TicksAbs) * map(__instance).weatherManager.CurWindSpeedFactor;
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
                RimThreaded.plantSwayHead = plantSwayHead(__instance);
                RimThreaded.plantMaterialsCount = plantMaterials.Count;
            }
            return false;
        }

        //public static List<Material> plantMaterialsList;
        public static int plantMaterialsCount;

        public static void WindManagerPrepare()
        {
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                maps[i].MapPreTick();
            }
        }

        public static void WindManagerListTick()
        {
            while (true)
            {
                int index = Interlocked.Decrement(ref plantMaterialsCount);
                if (index < 0) return;
                Material material = plantMaterials[index];
                try
                {
                    material.SetFloat(ShaderPropertyIDs.SwayHead, RimThreaded.plantSwayHead);
                }
                catch (Exception ex)
                {
                    Log.Error("Exception ticking " + WindManager_Patch.plantMaterials[index].ToStringSafe() + ": " + ex);
                }
            }
        }

    }
}

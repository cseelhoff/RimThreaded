using System;
using System.Collections.Generic;
using Verse;
using UnityEngine;
using System.Threading;

namespace RimThreaded
{

    public class WindManager_Patch
    {
        internal static void RunDestructivePatches()
        {
            Type original = typeof(WindManager);
            Type patched = typeof(WindManager_Patch);
            RimThreadedHarmony.Prefix(original, patched, "WindManagerTick");
        }

        public static bool WindManagerTick(WindManager __instance)
        {
            __instance.cachedWindSpeed = __instance.BaseWindSpeedAt(Find.TickManager.TicksAbs) * __instance.map.weatherManager.CurWindSpeedFactor;
            float curWindSpeedOffset = __instance.map.weatherManager.CurWindSpeedOffset;
            if (curWindSpeedOffset > 0f)
            {
                FloatRange floatRange = WindManager.WindSpeedRange * __instance.map.weatherManager.CurWindSpeedFactor;
                float num = (__instance.cachedWindSpeed - floatRange.min) / (floatRange.max - floatRange.min) * (floatRange.max - curWindSpeedOffset);
                __instance.cachedWindSpeed = curWindSpeedOffset + num;
            }

            List<Thing> list = __instance.map.listerThings.ThingsInGroup(ThingRequestGroup.WindSource);
            for (int i = 0; i < list.Count; i++)
            {
                CompWindSource compWindSource = list[i].TryGetComp<CompWindSource>();
                __instance.cachedWindSpeed = Mathf.Max(__instance.cachedWindSpeed, compWindSource.wind);
            }

            if (Prefs.PlantWindSway)
            {
                __instance.plantSwayHead += Mathf.Min(__instance.WindSpeed, 1f);
            }
            else
            {
                __instance.plantSwayHead = 0f;
            }

            if (Find.CurrentMap != __instance.map) return false;
            plantSwayHead = __instance.plantSwayHead;
            plantMaterialsCount = WindManager.plantMaterials.Count;
            return false;
        }

        //public static List<Material> plantMaterialsList;
        public static int plantMaterialsCount;
        public static float plantSwayHead;
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
                Material material = WindManager.plantMaterials[index];
                try
                {
                    material.SetFloat(ShaderPropertyIDs.SwayHead, plantSwayHead);
                }
                catch (Exception ex)
                {
                    Log.Error("Exception ticking " + WindManager.plantMaterials[index].ToStringSafe() + ": " + ex);
                }
            }
        }

    }
}

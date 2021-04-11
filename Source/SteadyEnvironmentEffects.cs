using System;
using RimWorld;
using Verse;
using UnityEngine;
using Verse.Noise;
using System.Threading;
using System.Reflection;
using static HarmonyLib.AccessTools;
using static RimThreaded.RimThreadedHarmony;

namespace RimThreaded
{

    public class SteadyEnvironmentEffects_Patch
    {
        public static FieldRef<SteadyEnvironmentEffects, Map> map = FieldRefAccess<SteadyEnvironmentEffects, Map>("map");
        public static FieldRef<SteadyEnvironmentEffects, float> outdoorMeltAmount = FieldRefAccess<SteadyEnvironmentEffects, float>("outdoorMeltAmount");
        public static FieldRef<SteadyEnvironmentEffects, float> snowRate = FieldRefAccess<SteadyEnvironmentEffects, float>("snowRate");
        public static FieldRef<SteadyEnvironmentEffects, float> rainRate = FieldRefAccess<SteadyEnvironmentEffects, float>("rainRate");
        public static FieldRef<SteadyEnvironmentEffects, float> deteriorationRate = FieldRefAccess<SteadyEnvironmentEffects, float>("deteriorationRate");
        public static FieldRef<SteadyEnvironmentEffects, int> cycleIndex = FieldRefAccess<SteadyEnvironmentEffects, int>("cycleIndex");
        public static FieldRef<SteadyEnvironmentEffects, ModuleBase> snowNoise = FieldRefAccess<SteadyEnvironmentEffects, ModuleBase>("snowNoise");

        private static readonly MethodInfo methodRollForRainFire =
            Method(typeof(SteadyEnvironmentEffects), "RollForRainFire", new Type[] { });
        private static readonly Action<SteadyEnvironmentEffects> actionRollForRainFire =
            (Action<SteadyEnvironmentEffects>)Delegate.CreateDelegate(
                typeof(Action<SteadyEnvironmentEffects>), methodRollForRainFire);

        private static readonly MethodInfo methodMeltAmountAt =
            Method(typeof(SteadyEnvironmentEffects), "MeltAmountAt", new Type[] { typeof(float) });
        private static readonly Func<SteadyEnvironmentEffects, float, float> funcMeltAmountAt =
            (Func<SteadyEnvironmentEffects, float, float>)Delegate.CreateDelegate(typeof(Func<SteadyEnvironmentEffects, float, float>), methodMeltAmountAt);


        internal static void RunDestructivePatches()
        {
            Type original = typeof(SteadyEnvironmentEffects);
            Type patched = typeof(SteadyEnvironmentEffects_Patch);
            Prefix(original, patched, "SteadyEnvironmentEffectsTick");
        }

        public static bool SteadyEnvironmentEffectsTick(SteadyEnvironmentEffects __instance)
        {
            Map map2 = map(__instance);
            if (Find.TickManager.TicksGame % 97f == 0f && Rand.Chance(0.02f))
            {
                actionRollForRainFire(__instance);
            }

            outdoorMeltAmount(__instance) = funcMeltAmountAt(__instance, map2.mapTemperature.OutdoorTemp);
            snowRate(__instance) = map2.weatherManager.SnowRate;
            rainRate(__instance) = map2.weatherManager.RainRate;
            deteriorationRate(__instance) = Mathf.Lerp(1f, 5f, rainRate(__instance));
            int area = map2.Area;
            int ticks = Mathf.CeilToInt(area * 0.0006f);
            int index = RimThreaded.steadyEnvironmentEffectsCount;
            RimThreaded.steadyEnvironmentEffectsStructures[index].steadyEnvironmentEffects = __instance;
            RimThreaded.steadyEnvironmentEffectsStructures[index].steadyEnvironmentEffectsCellsInRandomOrder = map2.cellsInRandomOrder;
            //int num = Mathf.CeilToInt((float)map2.Area * 0.0006f);
            RimThreaded.steadyEnvironmentEffectsStructures[index].steadyEnvironmentEffectsArea = area;
            //RimThreaded.steadyEnvironmentEffectsInstance = __instance;
            RimThreaded.steadyEnvironmentEffectsStructures[index].steadyEnvironmentEffectsCycleIndexOffset = ticks + cycleIndex(__instance);
            int ticks2 = Interlocked.Add(ref RimThreaded.totalSteadyEnvironmentEffectsTicks, ticks);
            RimThreaded.steadyEnvironmentEffectsStructures[index].steadyEnvironmentEffectsTicks = ticks2;
            Interlocked.Increment(ref RimThreaded.steadyEnvironmentEffectsCount);

            cycleIndex(__instance) = (cycleIndex(__instance) + ticks) % area;
            return false;
        }

    }
}

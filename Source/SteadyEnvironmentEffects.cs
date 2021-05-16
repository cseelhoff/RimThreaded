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
        public static FieldRef<SteadyEnvironmentEffects, int> cycleIndexFieldRef = FieldRefAccess<SteadyEnvironmentEffects, int>("cycleIndex");
        //public static FieldRef<SteadyEnvironmentEffects, ModuleBase> snowNoise = FieldRefAccess<SteadyEnvironmentEffects, ModuleBase>("snowNoise");

        private static readonly MethodInfo MethodRollForRainFire =
            Method(typeof(SteadyEnvironmentEffects), "RollForRainFire", new Type[] { });
        private static readonly Action<SteadyEnvironmentEffects> ActionRollForRainFire =
            (Action<SteadyEnvironmentEffects>)Delegate.CreateDelegate(
                typeof(Action<SteadyEnvironmentEffects>), MethodRollForRainFire);

        private static readonly MethodInfo MethodMeltAmountAt =
            Method(typeof(SteadyEnvironmentEffects), "MeltAmountAt", new [] { typeof(float) });
        private static readonly Func<SteadyEnvironmentEffects, float, float> FuncMeltAmountAt =
            (Func<SteadyEnvironmentEffects, float, float>)Delegate.CreateDelegate(typeof(Func<SteadyEnvironmentEffects, float, float>), MethodMeltAmountAt);
        
        public struct SteadyEnvironmentEffectsStructure
        {
            public SteadyEnvironmentEffects steadyEnvironmentEffects;
            public MapCellsInRandomOrder steadyEnvironmentEffectsCellsInRandomOrder;
            public int steadyEnvironmentEffectsTicks;
            public int steadyEnvironmentEffectsArea;
            public int steadyEnvironmentEffectsCycleIndexOffset;
        }

        public static SteadyEnvironmentEffectsStructure[] steadyEnvironmentEffectsStructures = new SteadyEnvironmentEffectsStructure[99];

        public static int totalSteadyEnvironmentEffectsTicks = 0;
        public static int steadyEnvironmentEffectsTicksCompleted = 0;
        public static int steadyEnvironmentEffectsCount = 0;


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
                ActionRollForRainFire(__instance);
            }

            outdoorMeltAmount(__instance) = FuncMeltAmountAt(__instance, map2.mapTemperature.OutdoorTemp);
            snowRate(__instance) = map2.weatherManager.SnowRate;
            rainRate(__instance) = map2.weatherManager.RainRate;
            deteriorationRate(__instance) = Mathf.Lerp(1f, 5f, rainRate(__instance));
            int area = map2.Area;
            int ticks = Mathf.CeilToInt(area * 0.0006f);
            int index = steadyEnvironmentEffectsCount;
            steadyEnvironmentEffectsStructures[index].steadyEnvironmentEffects = __instance;
            steadyEnvironmentEffectsStructures[index].steadyEnvironmentEffectsCellsInRandomOrder = map2.cellsInRandomOrder;
            //int num = Mathf.CeilToInt((float)map2.Area * 0.0006f);
            steadyEnvironmentEffectsStructures[index].steadyEnvironmentEffectsArea = area;
            //RimThreaded.steadyEnvironmentEffectsInstance = __instance;
            steadyEnvironmentEffectsStructures[index].steadyEnvironmentEffectsCycleIndexOffset = ticks + cycleIndexFieldRef(__instance);
            int ticks2 = Interlocked.Add(ref totalSteadyEnvironmentEffectsTicks, ticks);
            steadyEnvironmentEffectsStructures[index].steadyEnvironmentEffectsTicks = ticks2;
            Interlocked.Increment(ref steadyEnvironmentEffectsCount);
            cycleIndexFieldRef(__instance) = (cycleIndexFieldRef(__instance) + ticks) % area;
            return false;
        }
        
        private static readonly MethodInfo MethodDoCellSteadyEffects =
            Method(typeof(SteadyEnvironmentEffects), "DoCellSteadyEffects", new[] { typeof(IntVec3) });
        private static readonly Action<SteadyEnvironmentEffects, IntVec3> ActionDoCellSteadyEffects =
            (Action<SteadyEnvironmentEffects, IntVec3>)Delegate.CreateDelegate(
                typeof(Action<SteadyEnvironmentEffects, IntVec3>), MethodDoCellSteadyEffects);
        public static void SteadyEffectTick()
        {
            int steadyEnvironmentEffectsIndex = 0;
            while (true)
            {
                int ticketIndex = Interlocked.Increment(ref steadyEnvironmentEffectsTicksCompleted) - 1;
                if (ticketIndex >= totalSteadyEnvironmentEffectsTicks) return;
                int index = ticketIndex;
                while (ticketIndex >= steadyEnvironmentEffectsStructures[steadyEnvironmentEffectsIndex].steadyEnvironmentEffectsTicks)
                {
                    steadyEnvironmentEffectsIndex++;
                }
                if (steadyEnvironmentEffectsIndex > 0)
                    index = ticketIndex - steadyEnvironmentEffectsStructures[steadyEnvironmentEffectsIndex - 1].steadyEnvironmentEffectsTicks;
                int cycleIndex = (steadyEnvironmentEffectsStructures[steadyEnvironmentEffectsIndex].steadyEnvironmentEffectsCycleIndexOffset
                                  - index) % steadyEnvironmentEffectsStructures[steadyEnvironmentEffectsIndex].steadyEnvironmentEffectsArea;
                IntVec3 c = steadyEnvironmentEffectsStructures[steadyEnvironmentEffectsIndex].steadyEnvironmentEffectsCellsInRandomOrder.Get(cycleIndex);
                try
                {
                    ActionDoCellSteadyEffects(
                        steadyEnvironmentEffectsStructures[steadyEnvironmentEffectsIndex].steadyEnvironmentEffects, c);
                }
                catch (Exception ex)
                {
                    Log.Error("Exception ticking steadyEnvironmentEffectsCells " + index.ToStringSafe() + ": " + ex);
                }
            }
        }
    }
}

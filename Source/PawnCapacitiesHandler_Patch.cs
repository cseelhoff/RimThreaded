using HarmonyLib;
using System;
using System.Collections.Generic;
using Verse;
using System.Reflection;

namespace RimThreaded
{

    public class PawnCapacitiesHandler_Patch
    {
        internal static void RunDestructivePatches()
        {
            Type original = typeof(PawnCapacitiesHandler);
            Type patched = typeof(PawnCapacitiesHandler_Patch);
            RimThreadedHarmony.Prefix(original, patched, "Notify_CapacityLevelsDirty");
            RimThreadedHarmony.Prefix(original, patched, "Clear");
            RimThreadedHarmony.Prefix(original, patched, "CapableOf");
            ConstructorInfo constructorMethod = original.GetConstructor(new Type[] { typeof(Pawn) });
            MethodInfo cpMethod = patched.GetMethod("Postfix_Constructor");
            RimThreadedHarmony.harmony.Patch(constructorMethod, postfix: new HarmonyMethod(cpMethod));
        }
        
        public static Dictionary<PawnCapacitiesHandler, DefMap<PawnCapacityDef, CacheElement2>> cachedCapacityLevelsDict =
            new Dictionary<PawnCapacitiesHandler, DefMap<PawnCapacityDef, CacheElement2>>();
        
            public class CacheElement2        
        {
            public CacheStatus status;

            public float value;
        }
        public enum CacheStatus
        {
            Uncached,
            Caching,
            Cached
        }
        public static void Postfix_Constructor(PawnCapacitiesHandler __instance, Pawn pawn)
        {
            cachedCapacityLevelsDict[__instance] = new DefMap<PawnCapacityDef, CacheElement2>();
        }
        public static bool Clear(PawnCapacitiesHandler __instance)
        {
            cachedCapacityLevelsDict[__instance] = null;
            return false;
        }
        public static bool Notify_CapacityLevelsDirty(PawnCapacitiesHandler __instance)
        {
            if (cachedCapacityLevelsDict[__instance] == null)
            {
                cachedCapacityLevelsDict[__instance] = new DefMap<PawnCapacityDef, CacheElement2>();
            }

            for (int i = 0; i < cachedCapacityLevelsDict[__instance].Count; i++)
            {
                cachedCapacityLevelsDict[__instance][i].status = CacheStatus.Uncached;
            }
            return false;
        }
        
        public static bool CapableOf(PawnCapacitiesHandler __instance, ref bool __result, PawnCapacityDef capacity)
        {
            float levelResult = 0f;
            __result = false;
            if (capacity != null)
            {
                GetLevel(__instance, ref levelResult, capacity);
                __result = levelResult > capacity.minForCapable;
            }
            return false;
        }
        
        public static bool GetLevel(PawnCapacitiesHandler __instance, ref float __result, PawnCapacityDef capacity)
        {
            if (__instance.pawn.health.Dead)
            {
                __result = 0f;
                return false;
            }
            //if (cachedCapacityLevels == null) //REMOVED
            //CacheElement cacheElement = cachedCapacityLevels[capacity]; //REMOVED   
            
            __result = getCacheElementResult(__instance, capacity);
            return false;
        }

        private static float getCacheElementResult(PawnCapacitiesHandler __instance, PawnCapacityDef capacity)
        {
            if (capacity == null) //ADDED
            {
                return 0f;
            }
            CacheElement2 cacheElement = get_cacheElement(__instance, capacity); //ADDED
            lock (cacheElement) //ADDED
            {
                if (cacheElement.status == CacheStatus.Caching)
                {
                    Log.Error($"Detected infinite stat recursion when evaluating {capacity}");
                    return 0f;
                }

                if (cacheElement.status == CacheStatus.Uncached)
                {
                    cacheElement.status = CacheStatus.Caching;
                    try
                    {
                        cacheElement.value = PawnCapacityUtility.CalculateCapacityLevel(__instance.pawn.health.hediffSet, capacity);
                    }
                    finally
                    {
                        cacheElement.status = CacheStatus.Cached;
                    }
                }
            }
            return cacheElement.value;
        }

        private static CacheElement2 get_cacheElement(PawnCapacitiesHandler __instance, PawnCapacityDef capacity)
        {
            DefMap<PawnCapacityDef, CacheElement2> defMap = cachedCapacityLevelsDict[__instance];
            if (defMap == null)
            {
                defMap = new DefMap<PawnCapacityDef, CacheElement2>();
                cachedCapacityLevelsDict[__instance] = defMap;
                for (int i = 0; i < defMap.Count; i++)
                {
                    defMap[i].status = CacheStatus.Uncached;
                }
            }
            return defMap[capacity];
        }


    }
}

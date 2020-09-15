using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace RimThreaded
{

    public class PawnCapacitiesHandler_Patch
    {
        public static AccessTools.FieldRef<PawnCapacitiesHandler, Pawn> pawn =
            AccessTools.FieldRefAccess<PawnCapacitiesHandler, Pawn>("pawn");
        public static Dictionary<PawnCapacitiesHandler, DefMap<PawnCapacityDef, CacheElement>> cachedCapacityLevelsDict =
            new Dictionary<PawnCapacitiesHandler, DefMap<PawnCapacityDef, CacheElement>>();
        
            public class CacheElement        
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
            cachedCapacityLevelsDict[__instance] = new DefMap<PawnCapacityDef, CacheElement>();
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
                cachedCapacityLevelsDict[__instance] = new DefMap<PawnCapacityDef, CacheElement>();
            }

            for (int i = 0; i < cachedCapacityLevelsDict[__instance].Count; i++)
            {
                cachedCapacityLevelsDict[__instance][i].status = CacheStatus.Uncached;
            }
            return false;
        }
        /*
        public static bool CapableOf(PawnCapacitiesHandler __instance, ref bool __result, PawnCapacityDef capacity)
        {
            float levelResult = 0f;
            GetLevel(__instance, ref levelResult, capacity);
            __result = levelResult > capacity.minForCapable;
            return false;
        }
        */
        public static bool GetLevel(PawnCapacitiesHandler __instance, ref float __result, PawnCapacityDef capacity)
        {
            if (pawn(__instance).health.Dead)
            {
                __result = 0f;
                return false;
            }

            if (cachedCapacityLevelsDict[__instance] == null)
            {
                Notify_CapacityLevelsDirty(__instance);
            }

            CacheElement cacheElement = cachedCapacityLevelsDict[__instance][capacity];
            lock (cacheElement)
            {
                if (cacheElement.status == CacheStatus.Caching)
                {
                    Log.Error($"Detected infinite stat recursion when evaluating {capacity}");
                    __result = 0f;
                    return false;
                }

                if (cacheElement.status == CacheStatus.Uncached)
                {
                    cacheElement.status = CacheStatus.Caching;
                    try
                    {
                        cacheElement.value = PawnCapacityUtility.CalculateCapacityLevel(pawn(__instance).health.hediffSet, capacity);
                    }
                    finally
                    {
                        cacheElement.status = CacheStatus.Cached;
                    }
                }
            }

            __result = cacheElement.value;
            return false;
        }


    }
}

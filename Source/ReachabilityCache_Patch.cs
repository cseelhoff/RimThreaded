using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace RimThreaded
{
    class ReachabilityCache_Patch
    {
        public static void RunDestructivePatches()
        {
            Type original = typeof(ReachabilityCache);
            Type patched = typeof(ReachabilityCache_Patch);
            RimThreadedHarmony.Prefix(original, patched, "CachedResultFor");
            RimThreadedHarmony.Prefix(original, patched, "AddCachedResult");
            RimThreadedHarmony.Prefix(original, patched, "Clear");
        }
        
        public static bool CachedResultFor(ReachabilityCache __instance, ref BoolUnknown __result, Room A, Room B, TraverseParms traverseParams)
        {
            if (!__instance.cacheDict.TryGetValue(new ReachabilityCache.CachedEntry(A.ID, B.ID, traverseParams), out bool flag))
            {
                __result = BoolUnknown.Unknown;
                return false;
            }
            __result = !flag ? BoolUnknown.False : BoolUnknown.True;
            return false;
        }
        public static bool AddCachedResult(ReachabilityCache __instance, Room A, Room B, TraverseParms traverseParams, bool reachable)
        {
            ReachabilityCache.CachedEntry key = new ReachabilityCache.CachedEntry(A.ID, B.ID, traverseParams);
            if (__instance.cacheDict.ContainsKey(key))
                return false;
            lock (__instance.cacheDict)
            {
                if (!__instance.cacheDict.ContainsKey(key))
                    __instance.cacheDict.Add(key, reachable);
            }
            return false;
        }

        public static bool Clear(ReachabilityCache __instance)
        {
            lock (__instance.cacheDict)
            {
                __instance.cacheDict = new Dictionary<ReachabilityCache.CachedEntry, bool>();
            }
            return false;
        }

    }
}

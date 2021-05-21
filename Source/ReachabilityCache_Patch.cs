using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace RimThreaded
{
    class ReachabilityCache_Patch
    {
        [ThreadStatic] public static List<ReachabilityCache.CachedEntry> tmpCachedEntries;

        public static void InitializeThreadStatics()
        {
            tmpCachedEntries = new List<ReachabilityCache.CachedEntry>();
        }

        public static void RunDestructivePatches()
        {
            Type original = typeof(ReachabilityCache);
            Type patched = typeof(ReachabilityCache_Patch);
            RimThreadedHarmony.Prefix(original, patched, "get_Count");
            RimThreadedHarmony.Prefix(original, patched, "CachedResultFor");
            RimThreadedHarmony.Prefix(original, patched, "AddCachedResult");
            RimThreadedHarmony.Prefix(original, patched, "Clear");
            RimThreadedHarmony.Prefix(original, patched, "ClearFor");
            RimThreadedHarmony.Prefix(original, patched, "ClearForHostile");
        }
        
        public static Dictionary<ReachabilityCache, Dictionary<ReachabilityCache.CachedEntry, bool>> cacheDictDict = new Dictionary<ReachabilityCache, Dictionary<ReachabilityCache.CachedEntry, bool>>();

        public static bool get_Count(ReachabilityCache __instance, ref int __result)
        {
            __result = getCacheDict(__instance).Count;
            return false;
        }

        public static Dictionary<ReachabilityCache.CachedEntry, bool> getCacheDict(ReachabilityCache __instance)
        {
            Dictionary<ReachabilityCache.CachedEntry, bool> cacheDict;
            lock (cacheDictDict)
            {
                if (!cacheDictDict.TryGetValue(__instance, out cacheDict)) {
                    cacheDict = new Dictionary<ReachabilityCache.CachedEntry, bool>();
                    cacheDictDict[__instance] = cacheDict;
                }
            }            
            return cacheDict;
        }

        public static bool CachedResultFor(ReachabilityCache __instance, ref BoolUnknown __result, Room A, Room B, TraverseParms traverseParams)
        {
            Dictionary<ReachabilityCache.CachedEntry, bool> cacheDict = getCacheDict(__instance);
            lock (cacheDict)
            {
                if (cacheDict.TryGetValue(new ReachabilityCache.CachedEntry(A.ID, B.ID, traverseParams), out bool value))
                {
                    if (!value)
                    {
                        __result = BoolUnknown.False;
                        return false;
                    }
                    __result = BoolUnknown.True;
                    return false;
                }
            }
            __result = BoolUnknown.Unknown;
            return false;
        }
        public static bool AddCachedResult(ReachabilityCache __instance, Room A, Room B, TraverseParms traverseParams, bool reachable)
        {
            ReachabilityCache.CachedEntry key = new ReachabilityCache.CachedEntry(A.ID, B.ID, traverseParams);
            Dictionary<ReachabilityCache.CachedEntry, bool> cacheDict = getCacheDict(__instance);
            if (!cacheDict.ContainsKey(key))
            {
                lock (cacheDict)
                {
                    if (!cacheDict.ContainsKey(key))
                    {
                        cacheDict.Add(key, reachable);
                    }
                }
            }
            return false;
        }

        public static bool Clear(ReachabilityCache __instance)
        {
            Dictionary<ReachabilityCache.CachedEntry, bool> cacheDict = getCacheDict(__instance);
            lock (cacheDict)
            {
                cacheDict.Clear();
            }
            return false;
        }

        public static bool ClearFor(ReachabilityCache __instance, Pawn p)
        {
            tmpCachedEntries.Clear();
            Dictionary<ReachabilityCache.CachedEntry, bool> cacheDict = getCacheDict(__instance);

            lock (cacheDict)
            {
                foreach (KeyValuePair<ReachabilityCache.CachedEntry, bool> item in cacheDict)
                {
                    if (item.Key.TraverseParms.pawn == p)
                    {
                        tmpCachedEntries.Add(item.Key);
                    }
                }

                for (int i = 0; i < tmpCachedEntries.Count; i++)
                {
                    cacheDict.Remove(tmpCachedEntries[i]);
                }
            }

            //tmpCachedEntries.Clear();
            return false;
        }
        public static bool ClearForHostile(ReachabilityCache __instance, Thing hostileTo)
        {
            if (tmpCachedEntries == null)
            {
                tmpCachedEntries = new List<ReachabilityCache.CachedEntry>();
            }
            else
            {
                tmpCachedEntries.Clear();
            }
            Dictionary<ReachabilityCache.CachedEntry, bool> cacheDict = getCacheDict(__instance);
            lock (cacheDict)
            {
                foreach (KeyValuePair<ReachabilityCache.CachedEntry, bool> item in cacheDict)
                {
                    Pawn pawn = item.Key.TraverseParms.pawn;
                    if (pawn != null && pawn.HostileTo(hostileTo))
                    {
                        tmpCachedEntries.Add(item.Key);
                    }
                }
                for (int i = 0; i < tmpCachedEntries.Count; i++)
                {
                    cacheDict.Remove(tmpCachedEntries[i]);
                }
            }

            //tmpCachedEntries.Clear();
            return false;
        }
    }
}

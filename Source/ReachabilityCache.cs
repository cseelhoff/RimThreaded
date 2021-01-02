using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Verse;

namespace RimThreaded
{
    class ReachabilityCache_Patch
    {
        public struct CachedEntry2 : IEquatable<CachedEntry2>
        {
            public int FirstRoomID
            {
                get;
                private set;
            }

            public int SecondRoomID
            {
                get;
                private set;
            }

            public TraverseParms TraverseParms
            {
                get;
                private set;
            }

            public CachedEntry2(int firstRoomID, int secondRoomID, TraverseParms traverseParms)
            {
                this = default(CachedEntry2);
                if (firstRoomID < secondRoomID)
                {
                    FirstRoomID = firstRoomID;
                    SecondRoomID = secondRoomID;
                }
                else
                {
                    FirstRoomID = secondRoomID;
                    SecondRoomID = firstRoomID;
                }

                TraverseParms = traverseParms;
            }

            public static bool operator ==(CachedEntry2 lhs, CachedEntry2 rhs)
            {
                return lhs.Equals(rhs);
            }

            public static bool operator !=(CachedEntry2 lhs, CachedEntry2 rhs)
            {
                return !lhs.Equals(rhs);
            }

            public override bool Equals(object obj)
            {
                if (!(obj is CachedEntry2))
                {
                    return false;
                }

                return Equals((CachedEntry2)obj);
            }

            public bool Equals(CachedEntry2 other)
            {
                if (FirstRoomID == other.FirstRoomID && SecondRoomID == other.SecondRoomID)
                {
                    return TraverseParms == other.TraverseParms;
                }

                return false;
            }

            public override int GetHashCode()
            {
                return Gen.HashCombineStruct(Gen.HashCombineInt(FirstRoomID, SecondRoomID), TraverseParms);
            }
        }
        public static Dictionary<ReachabilityCache, Dictionary<CachedEntry2, bool>> cacheDictDict = new Dictionary<ReachabilityCache, Dictionary<CachedEntry2, bool>>();

        public static Dictionary<int, List<CachedEntry2>> tmpCachedEntries2 = new Dictionary<int, List<CachedEntry2>>();

        public static bool get_Count(ReachabilityCache __instance, ref int __result)
        {
            __result = getCacheDict(__instance).Count;
            return false;
        }
        public static List<CachedEntry2> getTmpCachedEntries()
        {
            int tID = Thread.CurrentThread.ManagedThreadId;
            if (!tmpCachedEntries2.TryGetValue(tID, out List<CachedEntry2> tmpCachedEntries))
            {
                tmpCachedEntries = new List<CachedEntry2>(new List<CachedEntry2>());
                lock (tmpCachedEntries2)
                {
                    tmpCachedEntries2[tID] = tmpCachedEntries;
                }
            }
            return tmpCachedEntries;
        }

        public static Dictionary<CachedEntry2, bool> getCacheDict(ReachabilityCache __instance)
        {
            if(!cacheDictDict.TryGetValue(__instance, out Dictionary<CachedEntry2, bool> cacheDict)) {
                lock (cacheDictDict)
                {
                    if (!cacheDictDict.TryGetValue(__instance, out Dictionary<CachedEntry2, bool> cacheDict2))
                    {
                        cacheDict = new Dictionary<CachedEntry2, bool>();
                        cacheDictDict[__instance] = cacheDict;
                    }
                }
            }
            return cacheDict;
        }

        public static bool CachedResultFor(ReachabilityCache __instance, ref BoolUnknown __result, Room A, Room B, TraverseParms traverseParams)
        {
            Dictionary<CachedEntry2, bool> cacheDict = getCacheDict(__instance);
            if (cacheDict.TryGetValue(new CachedEntry2(A.ID, B.ID, traverseParams), out bool value))
            {
                if (!value)
                {
                    __result = BoolUnknown.False;
                    return false;
                }
                __result = BoolUnknown.True;
                return false;
            }
            __result = BoolUnknown.Unknown;
            return false;
        }
        public static bool AddCachedResult(ReachabilityCache __instance, Room A, Room B, TraverseParms traverseParams, bool reachable)
        {
            CachedEntry2 key = new CachedEntry2(A.ID, B.ID, traverseParams);
            Dictionary<CachedEntry2, bool> cacheDict = getCacheDict(__instance);
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
            Dictionary<CachedEntry2, bool> cacheDict = getCacheDict(__instance);
            lock (cacheDict)
            {
                cacheDict.Clear();
            }
            return false;
        }

        public static bool ClearFor(ReachabilityCache __instance, Pawn p)
        {
            List<CachedEntry2> tmpCachedEntries = getTmpCachedEntries();
            tmpCachedEntries.Clear();
            Dictionary<CachedEntry2, bool> cacheDict = getCacheDict(__instance);
            foreach (KeyValuePair<CachedEntry2, bool> item in cacheDict)
            {
                if (item.Key.TraverseParms.pawn == p)
                {
                    tmpCachedEntries.Add(item.Key);
                }
            }

            for (int i = 0; i < tmpCachedEntries.Count; i++)
            {
                lock (cacheDict)
                {
                    cacheDict.Remove(tmpCachedEntries[i]);
                }
            }

            //tmpCachedEntries.Clear();
            return false;
        }
        public static bool ClearForHostile(ReachabilityCache __instance, Thing hostileTo)
        {
            List<CachedEntry2> tmpCachedEntries = getTmpCachedEntries();
            tmpCachedEntries.Clear();
            Dictionary<CachedEntry2, bool> cacheDict = getCacheDict(__instance);
            lock (cacheDict)
            {
                foreach (KeyValuePair<CachedEntry2, bool> item in cacheDict)
                {
                    Pawn pawn = item.Key.TraverseParms.pawn;
                    if (pawn != null && pawn.HostileTo(hostileTo))
                    {
                        tmpCachedEntries.Add(item.Key);
                    }
                }
            }
            for (int i = 0; i < tmpCachedEntries.Count; i++)
            {
                lock (cacheDict)
                {
                    cacheDict.Remove(tmpCachedEntries[i]);
                }
            }

            //tmpCachedEntries.Clear();
            return false;
        }
    }
}

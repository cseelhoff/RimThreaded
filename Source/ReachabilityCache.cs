using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace RimThreaded
{
    class ReachabilityCache_Patch
    {
        [ThreadStatic] public static List<CachedEntry2> tmpCachedEntries;
        [ThreadStatic] public static Dictionary<CachedEntry2, bool> cacheDict;
        [ThreadStatic] public static Pawn pawn;

        public static void InitializeThreadStatics()
        {
            tmpCachedEntries = new List<CachedEntry2>();
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

        public static bool get_Count(ReachabilityCache __instance, ref int __result)
        {
            __result = getCacheDict(__instance).Count;
            return false;
        }

        public static Dictionary<CachedEntry2, bool> getCacheDict(ReachabilityCache __instance)
        {
            
      
                if (!cacheDictDict.TryGetValue(__instance, out cacheDict)) {
                    cacheDict = new Dictionary<CachedEntry2, bool>();
                    cacheDictDict[__instance] = cacheDict;
                }
                     
            return cacheDict;
        }

        public static bool CachedResultFor(ReachabilityCache __instance, ref BoolUnknown __result, Room A, Room B, TraverseParms traverseParams)
        {
            cacheDict = getCacheDict(__instance);

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
            lock (__instance)
            {
                CachedEntry2 key = new CachedEntry2(A.ID, B.ID, traverseParams);
                cacheDict = getCacheDict(__instance);

                if (cacheDict.ContainsKey(key) == false)
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
            cacheDict = getCacheDict(__instance); 
            cacheDict.Clear();
            return false;
        }

        public static bool ClearFor(ReachabilityCache __instance, Pawn p)
        {
            tmpCachedEntries.Clear();
            cacheDict = getCacheDict(__instance);


                foreach (KeyValuePair<CachedEntry2, bool> item in cacheDict)
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
            

            //tmpCachedEntries.Clear();
            return false;
        }
        public static bool ClearForHostile(ReachabilityCache __instance, Thing hostileTo)
        {
            
                if (tmpCachedEntries == null)
                {
                    tmpCachedEntries = new List<CachedEntry2>();
                }
                else
                {
                    tmpCachedEntries.Clear();
                }
                cacheDict = getCacheDict(__instance);

                foreach (KeyValuePair<CachedEntry2, bool> item in cacheDict)
                {
                    pawn = item.Key.TraverseParms.pawn;
                    if (pawn != null && pawn.HostileTo(hostileTo))
                    {
                        tmpCachedEntries.Add(item.Key);
                    }
                }
                for (int i = 0; i < tmpCachedEntries.Count; i++)
                {
                    cacheDict.Remove(tmpCachedEntries[i]);
                }
                
            //tmpCachedEntries.Clear();
            return false;
        }
    }
}

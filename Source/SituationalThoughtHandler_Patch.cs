using HarmonyLib;
using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using System.Reflection;
using static RimWorld.SituationalThoughtHandler;

namespace RimThreaded
{

    public class SituationalThoughtHandler_Patch
	{
        [ThreadStatic] public static HashSet<ThoughtDef> tmpCachedThoughts;

        internal static void RunDestructivePatches()
        {
            Type original = typeof(SituationalThoughtHandler);
            Type patched = typeof(SituationalThoughtHandler_Patch);
            RimThreadedHarmony.Prefix(original, patched, "AppendSocialThoughts");
            RimThreadedHarmony.Prefix(original, patched, "Notify_SituationalThoughtsDirty");
            RimThreadedHarmony.Prefix(original, patched, "RemoveExpiredThoughtsFromCache");
            RimThreadedHarmony.Prefix(original, patched, "CheckRecalculateSocialThoughts");
        }

        internal static void InitializeThreadStatics()
        {
            tmpCachedThoughts = new HashSet<ThoughtDef>();
        }

        public static bool AppendSocialThoughts(SituationalThoughtHandler __instance, Pawn otherPawn, List<ISocialThought> outThoughts)
        {
            __instance.CheckRecalculateSocialThoughts(otherPawn);
            if (__instance.cachedSocialThoughts.TryGetValue(otherPawn, out CachedSocialThoughts cachedSocialThought))
            {
                cachedSocialThought.lastQueryTick = Find.TickManager.TicksGame;
                List<Thought_SituationalSocial> activeThoughts = cachedSocialThought.activeThoughts;
                for (int index = 0; index < activeThoughts.Count; ++index)
                    outThoughts.Add((ISocialThought)activeThoughts[index]);
            }
            return false;
        }
        private static void CheckRecalculateSocialThoughts(SituationalThoughtHandler __instance, Pawn otherPawn)
        {
            int ticksGame = Find.TickManager.TicksGame;
            if (ticksGame - __instance.lastMoodThoughtsRecalculationTick < 100)
                return;
            __instance.lastMoodThoughtsRecalculationTick = ticksGame;
            try
            {
                tmpCachedThoughts.Clear();
                List<Thought_Situational> cachedThoughtsSnapshot = __instance.cachedThoughts;
                for (int index = 0; index < cachedThoughtsSnapshot.Count; ++index)
                {
                    cachedThoughtsSnapshot[index].RecalculateState();
                    tmpCachedThoughts.Add(cachedThoughtsSnapshot[index].def);
                }
                List<ThoughtDef> socialThoughtDefs = ThoughtUtility.situationalNonSocialThoughtDefs;
                int index1 = 0;
                for (int count = socialThoughtDefs.Count; index1 < count; ++index1)
                {
                    if (!tmpCachedThoughts.Contains(socialThoughtDefs[index1]))
                    {
                        Thought_Situational thought = __instance.TryCreateThought(socialThoughtDefs[index1]);
                        if (thought != null)
                        {
                            lock (__instance)
                            {
                                __instance.cachedThoughts.Add(thought);
                            }
                        }                            
                    }
                }
            }
            finally
            {
            }
        }

        public static bool Notify_SituationalThoughtsDirty(SituationalThoughtHandler __instance)
        {
            lock (__instance)
            {
                __instance.cachedThoughts = new List<Thought_Situational>();
                __instance.cachedSocialThoughts = new Dictionary<Pawn, CachedSocialThoughts>();
            }
            __instance.lastMoodThoughtsRecalculationTick = -99999;
            return false;
        }

        public static bool RemoveExpiredThoughtsFromCache(SituationalThoughtHandler __instance)
        {
            lock (__instance)
            {
                Dictionary<Pawn, CachedSocialThoughts> newCachedSocialThoughts = new Dictionary<Pawn, CachedSocialThoughts>(__instance.cachedSocialThoughts);
                newCachedSocialThoughts.RemoveAll(x => x.Value.Expired || x.Key.Discarded);
                __instance.cachedSocialThoughts = newCachedSocialThoughts;
            }
            return false;
        }

    }
}

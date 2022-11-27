using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using static RimWorld.SituationalThoughtHandler;

namespace RimThreaded.RW_Patches
{

    public class SituationalThoughtHandler_Patch
    {
        [ThreadStatic] public static HashSet<ThoughtDef> tmpCachedThoughts;
        [ThreadStatic] public static HashSet<ThoughtDef> tmpCachedSocialThoughts;

        internal static void RunDestructivePatches()
        {
            Type original = typeof(SituationalThoughtHandler);
            Type patched = typeof(SituationalThoughtHandler_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(AppendSocialThoughts));
            RimThreadedHarmony.Prefix(original, patched, nameof(Notify_SituationalThoughtsDirty));
            RimThreadedHarmony.Prefix(original, patched, nameof(RemoveExpiredThoughtsFromCache));
            RimThreadedHarmony.Prefix(original, patched, nameof(CheckRecalculateSocialThoughts));
            RimThreadedHarmony.Prefix(original, patched, nameof(CheckRecalculateMoodThoughts));
        }

        internal static void InitializeThreadStatics()
        {
            tmpCachedThoughts = new HashSet<ThoughtDef>();
            tmpCachedSocialThoughts = new HashSet<ThoughtDef>();
        }

        public static bool AppendSocialThoughts(SituationalThoughtHandler __instance, Pawn otherPawn, List<ISocialThought> outThoughts)
        {
            __instance.CheckRecalculateSocialThoughts(otherPawn);
            if (__instance.cachedSocialThoughts.TryGetValue(otherPawn, out CachedSocialThoughts cachedSocialThought))
            {
                cachedSocialThought.lastQueryTick = Find.TickManager.TicksGame;
                List<Thought_SituationalSocial> activeThoughts = cachedSocialThought.activeThoughts;
                for (int index = 0; index < activeThoughts.Count; ++index)
                    outThoughts.Add(activeThoughts[index]);
            }
            return false;
        }

        public static bool CheckRecalculateMoodThoughts(SituationalThoughtHandler __instance)
        {
            int ticksGame = Find.TickManager.TicksGame;
            if (ticksGame - __instance.lastMoodThoughtsRecalculationTick < 100)
                return false;
            __instance.lastMoodThoughtsRecalculationTick = ticksGame;
            try
            {
                tmpCachedThoughts.Clear();
                for (int index = 0; index < __instance.cachedThoughts.Count; ++index)
                {
                    __instance.cachedThoughts[index].RecalculateState();
                    tmpCachedThoughts.Add(__instance.cachedThoughts[index].def);
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
                            lock (__instance.cachedThoughts)
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
            return false;
        }
        public static bool CheckRecalculateSocialThoughts(SituationalThoughtHandler __instance, Pawn otherPawn)
        {
            //RT made change change in 2.7.4
            try
            {
                if (!__instance.cachedSocialThoughts.TryGetValue(otherPawn, out CachedSocialThoughts cachedSocialThoughts))
                {
                    lock (__instance.cachedSocialThoughts)
                    {
                        if (!__instance.cachedSocialThoughts.TryGetValue(otherPawn, out CachedSocialThoughts cachedSocialThoughts2))
                        {
                            cachedSocialThoughts2 = new CachedSocialThoughts();
                            __instance.cachedSocialThoughts.Add(otherPawn, cachedSocialThoughts2);
                        }
                        cachedSocialThoughts = cachedSocialThoughts2;
                    }
                }
                if (!cachedSocialThoughts.ShouldRecalculateState)
                    return false;
                cachedSocialThoughts.lastRecalculationTick = Find.TickManager.TicksGame;
                tmpCachedSocialThoughts.Clear();
                for (int index = 0; index < cachedSocialThoughts.thoughts.Count; ++index)
                {
                    Thought_SituationalSocial thought = cachedSocialThoughts.thoughts[index];
                    thought.RecalculateState();
                    tmpCachedSocialThoughts.Add(thought.def);
                }
                List<ThoughtDef> socialThoughtDefs = ThoughtUtility.situationalSocialThoughtDefs;
                int index1 = 0;
                for (int count = socialThoughtDefs.Count; index1 < count; ++index1)
                {
                    if (!tmpCachedSocialThoughts.Contains(socialThoughtDefs[index1]))
                    {
                        Thought_SituationalSocial socialThought = __instance.TryCreateSocialThought(socialThoughtDefs[index1], otherPawn);
                        if (socialThought != null)
                        {
                            lock (cachedSocialThoughts)
                            {
                                cachedSocialThoughts.thoughts.Add(socialThought);
                            }
                        }
                    }
                }
                //lock (__instance.cachedSocialThoughts)
                //{
                //__instance.cachedSocialThoughts[otherPawn].activeThoughts = new List<Thought_SituationalSocial>();
                List<Thought_SituationalSocial> activeThoughts = new List<Thought_SituationalSocial>();
                //}
                for (int index2 = 0; index2 < cachedSocialThoughts.thoughts.Count; ++index2)
                {
                    Thought_SituationalSocial thought = cachedSocialThoughts.thoughts[index2];
                    if (thought.Active)
                    {
                        activeThoughts.Add(thought);
                    }
                }
                //RT made change change in 2.7.4
                cachedSocialThoughts.activeThoughts = activeThoughts;
            }
            finally
            {
            }
            return false;
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
                RemoveAll(newCachedSocialThoughts, x => x.Value.Expired || x.Key.Discarded);
                __instance.cachedSocialThoughts = newCachedSocialThoughts;
            }
            return false;
        }
        public static int RemoveAll<Pawn, CachedSocialThoughts>(Dictionary<Pawn, CachedSocialThoughts> dictionary, Predicate<KeyValuePair<Pawn, CachedSocialThoughts>> predicate)
        {
            List<Pawn> list = null;
            try
            {
                foreach (KeyValuePair<Pawn, CachedSocialThoughts> item in dictionary)
                {
                    if (predicate(item))
                    {
                        if (list == null)
                        {
                            list = SimplePool_Patch<List<Pawn>>.Get();
                        }

                        list.Add(item.Key);
                    }
                }

                if (list != null)
                {
                    int i = 0;
                    for (int count = list.Count; i < count; i++)
                    {
                        dictionary.Remove(list[i]);
                    }

                    return list.Count;
                }

                return 0;
            }
            finally
            {
                if (list != null)
                {
                    list.Clear();
                    SimplePool_Patch<List<Pawn>>.Return(list);
                }
            }
        }
    }
}

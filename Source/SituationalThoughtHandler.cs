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

    public class SituationalThoughtHandler_Patch
	{
        public static Dictionary<SituationalThoughtHandler, Dictionary<Pawn, CachedSocialThoughts>> this_cachedSocialThoughts = 
            new Dictionary<SituationalThoughtHandler, Dictionary<Pawn, CachedSocialThoughts>>();

        public static AccessTools.FieldRef<SituationalThoughtHandler, int> lastMoodThoughtsRecalculationTick =
            AccessTools.FieldRefAccess<SituationalThoughtHandler, int>("lastMoodThoughtsRecalculationTick");
        public static AccessTools.FieldRef<SituationalThoughtHandler, List<Thought_Situational>> cachedThoughts =
            AccessTools.FieldRefAccess<SituationalThoughtHandler, List<Thought_Situational>>("cachedThoughts");

        public static void Postfix_Constructor(SituationalThoughtHandler __instance, Pawn pawn)
        {
            this_cachedSocialThoughts[__instance] = new Dictionary<Pawn, CachedSocialThoughts>();
        }

        public class CachedSocialThoughts
        {
            public List<Thought_SituationalSocial> thoughts = new List<Thought_SituationalSocial>();
            public List<Thought_SituationalSocial> activeThoughts = new List<Thought_SituationalSocial>();
            public int lastRecalculationTick = -99999;
            public int lastQueryTick = -99999;
            private const int ExpireAfterTicks = 300;

            public bool Expired
            {
                get
                {
                    return Find.TickManager.TicksGame - this.lastQueryTick >= 300;
                }
            }

            public bool ShouldRecalculateState
            {
                get
                {
                    return Find.TickManager.TicksGame - this.lastRecalculationTick >= 100;
                }
            }
        }
        public static bool AppendSocialThoughts(SituationalThoughtHandler __instance, Pawn otherPawn, List<ISocialThought> outThoughts)
        {
            CheckRecalculateSocialThoughts(__instance, otherPawn);
            if (!this_cachedSocialThoughts.ContainsKey(__instance))
                this_cachedSocialThoughts.Add(__instance, new Dictionary<Pawn, CachedSocialThoughts>());
            try
            {
                CachedSocialThoughts cachedSocialThought = this_cachedSocialThoughts[__instance][otherPawn];
                cachedSocialThought.lastQueryTick = Find.TickManager.TicksGame;
                List<Thought_SituationalSocial> activeThoughts = cachedSocialThought.activeThoughts;
                for (int index = 0; index < activeThoughts.Count; ++index)
                    outThoughts.Add((ISocialThought)activeThoughts[index]);
            }
            catch (KeyNotFoundException) { }
            return false;
        }
        private static void CheckRecalculateSocialThoughts(SituationalThoughtHandler __instance, Pawn otherPawn)
        {
            try
            {
                CachedSocialThoughts cachedSocialThoughts;
                if (!this_cachedSocialThoughts.ContainsKey(__instance))
                    this_cachedSocialThoughts.Add(__instance, new Dictionary<Pawn, CachedSocialThoughts>());
                if (!this_cachedSocialThoughts[__instance].TryGetValue(otherPawn, out cachedSocialThoughts))
                {
                    cachedSocialThoughts = new CachedSocialThoughts();
                    this_cachedSocialThoughts[__instance].SetOrAdd(otherPawn, cachedSocialThoughts);
                }
                if (!cachedSocialThoughts.ShouldRecalculateState)
                    return;
                cachedSocialThoughts.lastRecalculationTick = Find.TickManager.TicksGame;
                //this.tmpCachedSocialThoughts.Clear();
                HashSet<ThoughtDef> tmpCachedSocialThoughts = new HashSet<ThoughtDef>();
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
                        Thought_SituationalSocial socialThought = TryCreateSocialThought(__instance, socialThoughtDefs[index1], otherPawn);
                        if (socialThought != null)
                            cachedSocialThoughts.thoughts.Add(socialThought);
                    }
                }
                cachedSocialThoughts.activeThoughts.Clear();
                for (int index2 = 0; index2 < cachedSocialThoughts.thoughts.Count; ++index2)
                {
                    Thought_SituationalSocial thought = cachedSocialThoughts.thoughts[index2];
                    if (thought.Active)
                        cachedSocialThoughts.activeThoughts.Add(thought);
                }
            }
            finally
            {
            }
        }

        private static Thought_SituationalSocial TryCreateSocialThought(SituationalThoughtHandler __instance,
          ThoughtDef def,
          Pawn otherPawn)
        {
            Thought_SituationalSocial situationalSocial = (Thought_SituationalSocial)null;
            try
            {
                if (!ThoughtUtility.CanGetThought_NewTemp(__instance.pawn, def, false) || !def.Worker.CurrentSocialState(__instance.pawn, otherPawn).ActiveFor(def))
                    return (Thought_SituationalSocial)null;
                situationalSocial = (Thought_SituationalSocial)ThoughtMaker.MakeThought(def);
                situationalSocial.pawn = __instance.pawn;
                situationalSocial.otherPawn = otherPawn;
                situationalSocial.RecalculateState();
            }
            catch (Exception ex)
            {
                Log.Error("Exception while recalculating " + (object)def + " thought state for pawn " + (object)__instance.pawn + ": " + (object)ex, false);
            }
            return situationalSocial;
        }
        public static bool Notify_SituationalThoughtsDirty(SituationalThoughtHandler __instance)
        {
            cachedThoughts(__instance).Clear();
            if (!this_cachedSocialThoughts.ContainsKey(__instance))
                this_cachedSocialThoughts.Add(__instance, new Dictionary<Pawn, CachedSocialThoughts>());
            this_cachedSocialThoughts[__instance].Clear();
            lastMoodThoughtsRecalculationTick(__instance) = -99999;
            return false;
        }

        public static bool RemoveExpiredThoughtsFromCache(SituationalThoughtHandler __instance)
        {
            if (!this_cachedSocialThoughts.ContainsKey(__instance))
                this_cachedSocialThoughts.Add(__instance, new Dictionary<Pawn, CachedSocialThoughts>());
            Dictionary<Pawn, CachedSocialThoughts> this_cachedSocialThoughtsInstance = this_cachedSocialThoughts[__instance];
            this_cachedSocialThoughtsInstance.RemoveAll(x => x.Value.Expired || x.Key.Discarded);
            return false;
        }

    }
}

using HarmonyLib;
using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using System.Reflection;

namespace RimThreaded
{

    public class SituationalThoughtHandler_Patch
	{
        public static Dictionary<SituationalThoughtHandler, Dictionary<Pawn, CachedSocialThoughts>> this_cachedSocialThoughts = 
            new Dictionary<SituationalThoughtHandler, Dictionary<Pawn, CachedSocialThoughts>>();
        
        static readonly Type original = typeof(SituationalThoughtHandler);
        static readonly Type patched = typeof(SituationalThoughtHandler_Patch);

        public static void RunNonDestructivePatches()
        {
            ConstructorInfo constructorMethod = original.GetConstructor(new Type[] { typeof(Pawn) });
            MethodInfo cpMethod = patched.GetMethod("Postfix_Constructor");
            RimThreadedHarmony.harmony.Patch(constructorMethod, postfix: new HarmonyMethod(cpMethod));
        }

        internal static void RunDestructivePatches()
        {
            Type original = typeof(SituationalThoughtHandler);
            Type patched = typeof(SituationalThoughtHandler_Patch);
            RimThreadedHarmony.Prefix(original, patched, "AppendSocialThoughts");
            RimThreadedHarmony.Prefix(original, patched, "Notify_SituationalThoughtsDirty");
            RimThreadedHarmony.Prefix(original, patched, "RemoveExpiredThoughtsFromCache");
        }

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
                    outThoughts.Add(activeThoughts[index]);
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
            Thought_SituationalSocial situationalSocial = null;
            try
            {
                if (!ThoughtUtility.CanGetThought_NewTemp(__instance.pawn, def, false) || !def.Worker.CurrentSocialState(__instance.pawn, otherPawn).ActiveFor(def))
                    return null;
                situationalSocial = (Thought_SituationalSocial)ThoughtMaker.MakeThought(def);
                situationalSocial.pawn = __instance.pawn;
                situationalSocial.otherPawn = otherPawn;
                situationalSocial.RecalculateState();
            }
            catch (Exception ex)
            {
                Log.Error("Exception while recalculating " + def + " thought state for pawn " + __instance.pawn + ": " + ex, false);
            }
            return situationalSocial;
        }
        public static bool Notify_SituationalThoughtsDirty(SituationalThoughtHandler __instance)
        {
            __instance.cachedThoughts.Clear();
            if (!this_cachedSocialThoughts.ContainsKey(__instance))
                this_cachedSocialThoughts.Add(__instance, new Dictionary<Pawn, CachedSocialThoughts>());
            this_cachedSocialThoughts[__instance].Clear();
            __instance.lastMoodThoughtsRecalculationTick = -99999;
            return false;
        }

        public static bool RemoveExpiredThoughtsFromCache(SituationalThoughtHandler __instance)
        {
            if (!this_cachedSocialThoughts.ContainsKey(__instance))
                this_cachedSocialThoughts.Add(__instance, new Dictionary<Pawn, CachedSocialThoughts>());
            Dictionary<Pawn, CachedSocialThoughts> this_cachedSocialThoughtsInstance = this_cachedSocialThoughts[__instance];
            //this_cachedSocialThoughtsInstance.RemoveAll(x => x.Value.Expired || x.Key.Discarded);
            List<Pawn> RemoveList = new List<Pawn>();
            lock (this_cachedSocialThoughtsInstance) { 
                foreach(KeyValuePair<Pawn, CachedSocialThoughts> x in this_cachedSocialThoughtsInstance)
                {
                    if ((x.Value != null && x.Value.Expired) || (x.Key != null && x.Key.Discarded))
                    {
                        RemoveList.Add(x.Key);
                        continue;
                    }
                }
                foreach(Pawn r in RemoveList)
                {
                    this_cachedSocialThoughtsInstance.Remove(r);
                }
            }

            return false;
        }

    }
}

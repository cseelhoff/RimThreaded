using RimWorld;
using System;
using Verse;

namespace RimThreaded
{
    class Corpse_Patch
    {
        internal static void RunDestructivePatches()
        {
            Type original = typeof(Corpse);
            Type patched = typeof(Corpse_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(GiveObservedThought));
        }

        public static bool GiveObservedThought(Corpse __instance, ref Thought_Memory __result)
        {
            Pawn innerPawn = __instance.InnerPawn; //changed
            if (innerPawn != null && !innerPawn.RaceProps.Humanlike) //changed
            {
                __result = null;
                return false;
            }
            if (__instance.StoringThing() != null)
            {
                __result = null;
                return false;
            }
            Thought_MemoryObservation memoryObservation = !__instance.IsNotFresh() ? (Thought_MemoryObservation)ThoughtMaker.MakeThought(ThoughtDefOf.ObservedLayingCorpse) : (Thought_MemoryObservation)ThoughtMaker.MakeThought(ThoughtDefOf.ObservedLayingRottingCorpse);
            memoryObservation.Target = (Thing)__instance;
            __result =(Thought_Memory)memoryObservation;
            return false;
        }
    }
}

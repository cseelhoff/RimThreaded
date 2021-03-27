using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RimThreaded
{

    public class Pawn_MeleeVerbs_Patch
    {
        [ThreadStatic] public static List<VerbEntry> meleeVerbs;
        [ThreadStatic] public static List<Verb> verbsToAdd;

        public static void InitializeThreadStatics()
        {
            meleeVerbs = new List<VerbEntry>();
            verbsToAdd = new List<Verb>();
        }
        public static void RunNonDestructivePatches()
        {
            Type original = typeof(Pawn_MeleeVerbs);
            Type patched = typeof(Pawn_MeleeVerbs_Patch);
            RimThreadedHarmony.AddAllMatchingFields(original, patched);
            RimThreadedHarmony.TranspileFieldReplacements(original, "PawnMeleeVerbsStaticUpdate");
            RimThreadedHarmony.TranspileFieldReplacements(original, "GetUpdatedAvailableVerbsList");
        }

    }
}

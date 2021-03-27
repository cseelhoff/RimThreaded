using RimWorld;
using System;
using System.Collections.Generic;
using Verse;

namespace RimThreaded
{
    public class Pawn_WorkSettings_Patch
    {
        [ThreadStatic] public static List<WorkTypeDef> wtsByPrio;

        public static void InitializeThreadStatics()
        {
            wtsByPrio = new List<WorkTypeDef>();
        }
        internal static void RunNonDestructivePatches()
        {
            Type original = typeof(Pawn_WorkSettings);
            Type patched = typeof(Pawn_WorkSettings_Patch);
            RimThreadedHarmony.AddAllMatchingFields(original, patched);
            RimThreadedHarmony.TranspileFieldReplacements(original, "CacheWorkGiversInOrder");
        }

    }
}

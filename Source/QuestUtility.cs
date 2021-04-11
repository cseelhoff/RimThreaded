using System;
using System.Collections.Generic;
using RimWorld;

namespace RimThreaded
{

    public class QuestUtility_Patch
    {

        [ThreadStatic] public static List<ExtraFaction> tmpExtraFactions;
        public static void InitializeThreadStatics()
        {
            tmpExtraFactions = new List<ExtraFaction>();
        }

        internal static void RunNonDestructivePatches()
        {
            Type original = typeof(QuestUtility);
            Type patched = typeof(QuestUtility_Patch);
            RimThreadedHarmony.AddAllMatchingFields(original, patched);
            RimThreadedHarmony.TranspileFieldReplacements(original, "GetExtraFaction");
        }

        internal static void RunDestructivePatches()
        {
            Type original = typeof(QuestUtility);
            Type patched = typeof(QuestUtility_Patch);
            RimThreadedHarmony.Prefix(original, patched, "GetExtraFaction");
        }


    }
}
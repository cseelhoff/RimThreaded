using System;
using System.Collections.Generic;
using System.Text;
using RimWorld;
using Verse;

namespace RimThreaded
{

    public class JobGiver_OptimizeApparel_Patch
    {
        [ThreadStatic] public static NeededWarmth neededWarmth;
        [ThreadStatic] public static StringBuilder debugSb;
        [ThreadStatic] public static List<float> wornApparelScores;
        [ThreadStatic] public static HashSet<BodyPartGroupDef> tmpBodyPartGroupsWithRequirement;
        [ThreadStatic] public static HashSet<ThingDef> tmpAllowedApparels;
        [ThreadStatic] public static HashSet<ThingDef> tmpRequiredApparels;

        private static readonly Type Original = typeof(JobGiver_OptimizeApparel);
        private static readonly Type Patched = typeof(JobGiver_OptimizeApparel_Patch);

        public static void InitializeThreadStatics()
        {
            wornApparelScores = new List<float>();
            tmpBodyPartGroupsWithRequirement = new HashSet<BodyPartGroupDef>();
            tmpAllowedApparels = new HashSet<ThingDef>();
            tmpRequiredApparels = new HashSet<ThingDef>();
        }

        internal static void RunNonDestructivePatches()
        {
            RimThreadedHarmony.AddAllMatchingFields(Original, Patched);
            RimThreadedHarmony.TranspileFieldReplacements(Original, "TryGiveJob");
            RimThreadedHarmony.TranspileFieldReplacements(Original, "ApparelScoreRaw");
            RimThreadedHarmony.TranspileFieldReplacements(Original, "ApparelScoreGain");
        }


    }
}
using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RimThreaded
{

    public class JobGiver_ConfigurableHostilityResponse_Patch
    {
        [ThreadStatic] public static List<Thing> tmpThreats;
        internal static void InitializeThreadStatics()
        {
            tmpThreats = new List<Thing>();
        }

        internal static void RunNonDestructivePatches()
        {
            Type original = typeof(JobGiver_ConfigurableHostilityResponse);
            Type patched = typeof(JobGiver_ConfigurableHostilityResponse_Patch);
            RimThreadedHarmony.AddAllMatchingFields(original, patched);
            RimThreadedHarmony.TranspileFieldReplacements(original, "TryGetFleeJob");
        }
        
    }
}

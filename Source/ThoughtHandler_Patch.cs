using RimWorld;
using System;
using System.Collections.Generic;

namespace RimThreaded
{
    
    public class ThoughtHandler_Patch
    {
        [ThreadStatic] public static List<Thought> tmpThoughts;
        [ThreadStatic] public static List<Thought> tmpTotalMoodOffsetThoughts;
        [ThreadStatic] public static List<ISocialThought> tmpSocialThoughts;
        [ThreadStatic] public static List<ISocialThought> tmpTotalOpinionOffsetThoughts;

        public static void InitializeThreadStatics()
        {
            tmpThoughts = new List<Thought>();
            tmpTotalMoodOffsetThoughts = new List<Thought>();
            tmpSocialThoughts = new List<ISocialThought>();
            tmpTotalOpinionOffsetThoughts = new List<ISocialThought>();
        }

        public static void RunNonDestructivePatches()
        {
            Type original = typeof(ThoughtHandler);
            Type patched = typeof(ThoughtHandler_Patch);
            RimThreadedHarmony.AddAllMatchingFields(original, patched);
            RimThreadedHarmony.TranspileFieldReplacements(original, "MoodOffsetOfGroup");
            RimThreadedHarmony.TranspileFieldReplacements(original, "TotalMoodOffset");
            RimThreadedHarmony.TranspileFieldReplacements(original, "OpinionOffsetOfGroup");
            RimThreadedHarmony.TranspileFieldReplacements(original, "TotalOpinionOffset");
        }

    }

}

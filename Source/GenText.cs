using System;
using System.Text;
using Verse;

namespace RimThreaded
{

    public class GenText_Patch
	{
        [ThreadStatic] public static StringBuilder tmpSbForCapitalizedSentences;

        public static void InitializedThreadStatics()
        {
            tmpSbForCapitalizedSentences = new StringBuilder();
        }

        internal static void RunNonDestructivePatches()
        {
            Type original = typeof(GenText);
            Type patched = typeof(GenText_Patch);
            RimThreadedHarmony.AddAllMatchingFields(original, patched);
            RimThreadedHarmony.TranspileFieldReplacements(original, "CapitalizeSentences");
        }
    }
}

using System;
using System.Text;
using Verse;

namespace RimThreaded
{

    public class LanguageWordInfo_Patch
	{
        [ThreadStatic] public static StringBuilder tmpLowercase;
        internal static void InitializeThreadStatics()
        {
            tmpLowercase = new StringBuilder();
        }
        internal static void RunNonDestructivePatches()
        {
            Type original = typeof(LanguageWordInfo);
            Type patched = typeof(LanguageWordInfo_Patch);
            RimThreadedHarmony.AddAllMatchingFields(original, patched);
            RimThreadedHarmony.TranspileFieldReplacements(original, "TryResolveGender");
        }

    }
}

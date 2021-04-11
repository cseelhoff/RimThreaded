using System;
using System.Collections.Generic;
using Verse;

namespace RimThreaded
{

    public class GrammarResolverSimpleStringExtensions_Patch
	{

        [ThreadStatic] public static List<string> argsLabels = new List<string>();
        [ThreadStatic] public static List<object> argsObjects = new List<object>();
        internal static void InitializeThreadStatics()
        {
            argsLabels = new List<string>();
            argsObjects = new List<object>();
        }
        internal static void RunNonDestructivePatches()
        {
            Type original = typeof(GrammarResolverSimpleStringExtensions);
            Type patched = typeof(GrammarResolverSimpleStringExtensions_Patch);
            RimThreadedHarmony.AddAllMatchingFields(original, patched);
            RimThreadedHarmony.TranspileFieldReplacements(original, "Formatted", new Type[] { typeof(string), typeof(NamedArgument) });
            RimThreadedHarmony.TranspileFieldReplacements(original, "Formatted", new Type[] { typeof(string), typeof(NamedArgument), typeof(NamedArgument) });
            RimThreadedHarmony.TranspileFieldReplacements(original, "Formatted", new Type[] { typeof(string), typeof(NamedArgument), typeof(NamedArgument), typeof(NamedArgument) });
            RimThreadedHarmony.TranspileFieldReplacements(original, "Formatted", new Type[] { typeof(string), typeof(NamedArgument), typeof(NamedArgument), typeof(NamedArgument), typeof(NamedArgument) });
            RimThreadedHarmony.TranspileFieldReplacements(original, "Formatted", new Type[] { typeof(string), typeof(NamedArgument), typeof(NamedArgument), typeof(NamedArgument), typeof(NamedArgument), typeof(NamedArgument) });
            RimThreadedHarmony.TranspileFieldReplacements(original, "Formatted", new Type[] { typeof(string), typeof(NamedArgument), typeof(NamedArgument), typeof(NamedArgument), typeof(NamedArgument), typeof(NamedArgument), typeof(NamedArgument) });
            RimThreadedHarmony.TranspileFieldReplacements(original, "Formatted", new Type[] { typeof(string), typeof(NamedArgument), typeof(NamedArgument), typeof(NamedArgument), typeof(NamedArgument), typeof(NamedArgument), typeof(NamedArgument), typeof(NamedArgument) });
            RimThreadedHarmony.TranspileFieldReplacements(original, "Formatted", new Type[] { typeof(string), typeof(NamedArgument), typeof(NamedArgument), typeof(NamedArgument), typeof(NamedArgument), typeof(NamedArgument), typeof(NamedArgument), typeof(NamedArgument), typeof(NamedArgument) });
            RimThreadedHarmony.TranspileFieldReplacements(original, "Formatted", new Type[] { typeof(string), typeof(NamedArgument[]) });
                    }

    }
}

using System;
using System.Collections.Generic;
using Verse;
using Verse.Grammar;

namespace RimThreaded
{
    class RulePackDef_Patch
    {
        internal static void RunDestructivePatches()
        {
            Type original = typeof(RulePackDef);
            Type patched = typeof(RulePackDef_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(get_RulesPlusIncludes));
        }
        public static bool get_RulesPlusIncludes(RulePackDef __instance, ref List<Rule> __result)
        {
            if (__instance.cachedRules == null)
            {
                lock (__instance)
                {
                    List<Rule> tmpCachedRules = new List<Rule>(); //changed
                    if (__instance.rulePack != null)
                        tmpCachedRules.AddRange((IEnumerable<Rule>)__instance.rulePack.Rules); //changed
                    if (__instance.include != null)
                    {
                        for (int index = 0; index < __instance.include.Count; ++index)
                            tmpCachedRules.AddRange((IEnumerable<Rule>)__instance.include[index].RulesPlusIncludes); //changed
                    }
                    __instance.cachedRules = tmpCachedRules; //changed
                }
            }
            __result = __instance.cachedRules;
            return false;
        }
    }
}

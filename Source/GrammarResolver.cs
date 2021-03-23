using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using Verse.Grammar;
using static HarmonyLib.AccessTools;
using System.Text.RegularExpressions;
using UnityEngine;

namespace RimThreaded
{

    public class GrammarResolver_Patch
    {
        public static int loopCount = StaticFieldRefAccess<int>(typeof(GrammarResolver), "loopCount");
        public static Regex Spaces = StaticFieldRefAccess<Regex>(typeof(GrammarResolver), "Spaces");
        public static StringBuilder logSbTrace = StaticFieldRefAccess<StringBuilder>(typeof(GrammarResolver), "logSbTrace");
        public static StringBuilder logSbMid = StaticFieldRefAccess<StringBuilder>(typeof(GrammarResolver), "logSbMid");
        public static StringBuilder logSbRules = StaticFieldRefAccess<StringBuilder>(typeof(GrammarResolver), "logSbRules");
        public static Dictionary<string, List<RuleEntry>> rules = new Dictionary<string, List<RuleEntry>>();
        static object resolveLock = new object();

        private static bool TryResolveRecursive(RuleEntry entry, int depth, Dictionary<string, string> constants, out string output, bool log, List<string> extraTags, List<string> resolvedTags)
        {
            string text = "";
            for (int i = 0; i < depth; i++)
            {
                text += "  ";
            }

            if (log && depth > 0)
            {
                logSbTrace.AppendLine();
                logSbTrace.Append(depth.ToStringCached().PadRight(3));
                logSbTrace.Append(text + entry);
            }

            text += "     ";
            loopCount++;
            if (loopCount > 1000)
            {
                Log.Error("Hit loops limit resolving grammar.");
                output = "HIT_LOOPS_LIMIT";
                if (log)
                {
                    logSbTrace.Append("\n" + text + "UNRESOLVABLE: Hit loops limit");
                }

                return false;
            }

            if (depth > 50)
            {
                Log.Error("Grammar recurred too deep while resolving keyword (>" + 50 + " deep)");
                output = "DEPTH_LIMIT_REACHED";
                if (log)
                {
                    logSbTrace.Append("\n" + text + "UNRESOLVABLE: Depth limit reached");
                }

                return false;
            }

            string text2 = entry.rule.Generate();
            bool flag = false;
            int num = -1;
            for (int j = 0; j < text2.Length; j++)
            {
                char num2 = text2[j];
                if (num2 == '[')
                {
                    num = j;
                }

                if (num2 != ']')
                {
                    continue;
                }

                if (num == -1)
                {
                    Log.Error("Could not resolve rule because of mismatched brackets: " + text2);
                    output = "MISMATCHED_BRACKETS";
                    if (log)
                    {
                        logSbTrace.Append("\n" + text + "UNRESOLVABLE: Mismatched brackets");
                    }

                    flag = true;
                    continue;
                }

                string text3 = text2.Substring(num + 1, j - num - 1);
                while (true)
                {
                    RuleEntry ruleEntry = RandomPossiblyResolvableEntry(text3, constants, extraTags, resolvedTags);
                    if (ruleEntry == null)
                    {
                        entry.MarkKnownUnresolvable();
                        output = "CANNOT_RESOLVE_SUBSYMBOL:" + text3;
                        if (log)
                        {
                            logSbTrace.Append("\n" + text + text3 + " → UNRESOLVABLE");
                        }

                        flag = true;
                        break;
                    }

                    ruleEntry.uses++;
                    List<string> list = resolvedTags.ToList();
                    if (TryResolveRecursive(ruleEntry, depth + 1, constants, out string output2, log, extraTags, list))
                    {
                        text2 = text2.Substring(0, num) + output2 + text2.Substring(j + 1);
                        j = num;
                        resolvedTags.Clear();
                        resolvedTags.AddRange(list);
                        if (!ruleEntry.rule.tag.NullOrEmpty() && !resolvedTags.Contains(ruleEntry.rule.tag))
                        {
                            resolvedTags.Add(ruleEntry.rule.tag);
                        }

                        break;
                    }

                    ruleEntry.MarkKnownUnresolvable();
                }
            }

            output = text2;
            return !flag;
        }


        public class RuleEntry
        {
            public Rule rule;

            public bool knownUnresolvable;

            public bool constantConstraintsChecked;

            public bool constantConstraintsValid;

            public int uses;

            public float SelectionWeight => rule.BaseSelectionWeight * 100000f / ((uses + 1) * 1000);

            public float Priority => rule.Priority;

            public RuleEntry(Rule rule)
            {
                this.rule = rule;
                knownUnresolvable = false;
            }

            public void MarkKnownUnresolvable()
            {
                knownUnresolvable = true;
            }

            public bool ValidateConstantConstraints(Dictionary<string, string> constraints)
            {
                if (!constantConstraintsChecked)
                {
                    constantConstraintsValid = true;
                    if (rule.constantConstraints != null)
                    {
                        for (int i = 0; i < rule.constantConstraints.Count; i++)
                        {
                            Rule.ConstantConstraint constantConstraint = rule.constantConstraints[i];
                            string text = (constraints != null) ? constraints.TryGetValue(constantConstraint.key, "") : "";
                            float result = 0f;
                            float result2 = 0f;
                            bool flag = !text.NullOrEmpty() && !constantConstraint.value.NullOrEmpty() && float.TryParse(text, out result) && float.TryParse(constantConstraint.value, out result2);
                            bool flag2;
                            switch (constantConstraint.type)
                            {
                                case Rule.ConstantConstraint.Type.Equal:
                                    flag2 = text.EqualsIgnoreCase(constantConstraint.value);
                                    break;
                                case Rule.ConstantConstraint.Type.NotEqual:
                                    flag2 = !text.EqualsIgnoreCase(constantConstraint.value);
                                    break;
                                case Rule.ConstantConstraint.Type.Less:
                                    flag2 = (flag && result < result2);
                                    break;
                                case Rule.ConstantConstraint.Type.Greater:
                                    flag2 = (flag && result > result2);
                                    break;
                                case Rule.ConstantConstraint.Type.LessOrEqual:
                                    flag2 = (flag && result <= result2);
                                    break;
                                case Rule.ConstantConstraint.Type.GreaterOrEqual:
                                    flag2 = (flag && result >= result2);
                                    break;
                                default:
                                    Log.Error("Unknown ConstantConstraint type: " + constantConstraint.type);
                                    flag2 = false;
                                    break;
                            }

                            if (!flag2)
                            {
                                constantConstraintsValid = false;
                                break;
                            }
                        }
                    }

                    constantConstraintsChecked = true;
                }

                return constantConstraintsValid;
            }

            public bool ValidateRequiredTag(List<string> extraTags, List<string> resolvedTags)
            {
                if (rule.requiredTag.NullOrEmpty())
                {
                    return true;
                }

                if (extraTags != null && extraTags.Contains(rule.requiredTag))
                {
                    return true;
                }

                return resolvedTags.Contains(rule.requiredTag);
            }

            public override string ToString()
            {
                return rule.ToString();
            }
        }

        private static bool AddRule(Rule rule)
        {
            List<RuleEntry> value = null;
            if (!rules.TryGetValue(rule.keyword, out value))
            {
                value = new List<RuleEntry>();
                //value.Clear();
                lock (rules)
                {
                    rules[rule.keyword] = value;
                }
            }

            value.Add(new RuleEntry(rule));
            return false;
        }
        public static bool ResolveUnsafe(ref string __result, string rootKeyword, GrammarRequest request, out bool success, string debugLabel = null, bool forceLog = false, bool useUntranslatedRules = false, List<string> extraTags = null, List<string> outTags = null, bool capitalizeFirstSentence = true)
        {
            string output;
            lock (resolveLock)
            {
                bool flag = forceLog || DebugViewSettings.logGrammarResolution;

                rules.Clear();

                //rulePool.Clear();
                if (flag)
                {
                    logSbTrace = new StringBuilder();
                    logSbMid = new StringBuilder();
                    logSbRules = new StringBuilder();
                }

                List<Rule> rulesAllowNull = request.RulesAllowNull;
                if (rulesAllowNull != null)
                {
                    if (flag)
                    {
                        logSbRules.AppendLine("CUSTOM RULES");
                    }

                    for (int i = 0; i < rulesAllowNull.Count; i++)
                    {
                        AddRule(rulesAllowNull[i]);
                        if (flag)
                        {
                            logSbRules.AppendLine("■" + rulesAllowNull[i].ToString());
                        }
                    }

                    if (flag)
                    {
                        logSbRules.AppendLine();
                    }
                }

                List<RulePackDef> includesAllowNull = request.IncludesAllowNull;
                if (includesAllowNull != null)
                {
                    HashSet<RulePackDef> hashSet = new HashSet<RulePackDef>();
                    List<RulePackDef> list = new List<RulePackDef>(includesAllowNull);
                    if (flag)
                    {
                        logSbMid.AppendLine("INCLUDES");
                    }

                    while (list.Count > 0)
                    {
                        RulePackDef rulePackDef = list[list.Count - 1];
                        list.RemoveLast();
                        if (!hashSet.Contains(rulePackDef))
                        {
                            if (flag)
                            {
                                logSbMid.AppendLine($"{rulePackDef.defName}");
                            }

                            hashSet.Add(rulePackDef);
                            List<Rule> list2 = useUntranslatedRules ? rulePackDef.UntranslatedRulesImmediate : rulePackDef.RulesImmediate;
                            if (list2 != null)
                            {
                                foreach (Rule item in list2)
                                {
                                    AddRule(item);
                                }
                            }

                            if (!rulePackDef.include.NullOrEmpty())
                            {
                                list.AddRange(rulePackDef.include);
                            }
                        }
                    }
                }

                List<RulePack> includesBareAllowNull = request.IncludesBareAllowNull;
                if (includesBareAllowNull != null)
                {
                    if (flag)
                    {
                        logSbMid.AppendLine();
                        logSbMid.AppendLine("BARE INCLUDES");
                    }

                    for (int j = 0; j < includesBareAllowNull.Count; j++)
                    {
                        List<Rule> list3 = useUntranslatedRules ? includesBareAllowNull[j].UntranslatedRules : includesBareAllowNull[j].Rules;
                        for (int k = 0; k < list3.Count; k++)
                        {
                            AddRule(list3[k]);
                            if (flag)
                            {
                                logSbMid.AppendLine("  " + list3[k].ToString());
                            }
                        }
                    }
                }

                if (flag && !extraTags.NullOrEmpty())
                {
                    logSbMid.AppendLine();
                    logSbMid.AppendLine("EXTRA TAGS");
                    for (int l = 0; l < extraTags.Count; l++)
                    {
                        logSbMid.AppendLine("  " + extraTags[l]);
                    }
                }

                List<Rule> list4 = useUntranslatedRules ? RulePackDefOf.GlobalUtility.UntranslatedRulesPlusIncludes : RulePackDefOf.GlobalUtility.RulesPlusIncludes;
                for (int m = 0; m < list4.Count; m++)
                {
                    AddRule(list4[m]);
                }

                loopCount = 0;
                Dictionary<string, string> constantsAllowNull = request.ConstantsAllowNull;
                if (flag && constantsAllowNull != null)
                {
                    logSbMid.AppendLine("CONSTANTS");
                    foreach (KeyValuePair<string, string> item2 in constantsAllowNull)
                    {
                        logSbMid.AppendLine(item2.Key.PadRight(38) + " " + item2.Value);
                    }
                }

                if (flag)
                {
                    logSbTrace.Append("GRAMMAR RESOLUTION TRACE");
                }

                output = "err";
                bool flag2 = false;
                List<string> list5 = new List<string>();
                if (TryResolveRecursive(new RuleEntry(new Rule_String("", "[" + rootKeyword + "]")), 0, constantsAllowNull, out output, flag, extraTags, list5))
                {
                    if (outTags != null)
                    {
                        outTags.Clear();
                        outTags.AddRange(list5);
                    }
                }
                else
                {
                    flag2 = true;
                    output = ((!request.Rules.NullOrEmpty()) ? ("ERR: " + request.Rules[0].Generate()) : "ERR");
                    if (flag)
                    {
                        logSbTrace.Insert(0, "Grammar unresolvable. Root '" + rootKeyword + "'\n\n");
                    }
                    else
                    {
                        GrammarResolver.ResolveUnsafe(rootKeyword, request, debugLabel, forceLog: true, useUntranslatedRules, extraTags);
                    }
                }

                output = GenText.CapitalizeSentences(Find.ActiveLanguageWorker.PostProcessed(output), capitalizeFirstSentence);
                output = Spaces.Replace(output, (Match match) => match.Groups[1].Value);
                output = output.Trim();
                if (flag)
                {
                    StringBuilder stringBuilder = new StringBuilder();
                    stringBuilder.Append(logSbTrace.ToString().TrimEndNewlines());
                    stringBuilder.AppendLine();
                    stringBuilder.AppendLine();
                    stringBuilder.Append(logSbMid.ToString().TrimEndNewlines());
                    stringBuilder.AppendLine();
                    stringBuilder.AppendLine();
                    stringBuilder.Append(logSbRules.ToString().TrimEndNewlines());
                    if (flag2)
                    {
                        if (DebugViewSettings.logGrammarResolution)
                        {
                            Log.Error(stringBuilder.ToString().Trim() + "\n");
                        }
                        else
                        {
                            Log.ErrorOnce(stringBuilder.ToString().Trim() + "\n", stringBuilder.ToString().Trim().GetHashCode());
                        }
                    }
                    else
                    {
                        Log.Message(stringBuilder.ToString().Trim() + "\n");
                    }

                    logSbTrace = null;
                    logSbMid = null;
                    logSbRules = null;
                }

                success = !flag2;
            }
            __result = output;
            return false;

        }
        private static RuleEntry RandomPossiblyResolvableEntry(string keyword, Dictionary<string, string> constants, List<string> extraTags, List<string> resolvedTags)
        {
            List<RuleEntry> list = rules.TryGetValue(keyword);
            if (list == null)
            {
                return null;
            }

            float maxPriority = float.MinValue;
            for (int i = 0; i < list.Count; i++)
            {
                RuleEntry ruleEntry = list[i];
                if (!ruleEntry.knownUnresolvable && ruleEntry.ValidateConstantConstraints(constants) && ruleEntry.ValidateRequiredTag(extraTags, resolvedTags) && ruleEntry.SelectionWeight != 0f)
                {
                    maxPriority = Mathf.Max(maxPriority, ruleEntry.Priority);
                }
            }

            return list.RandomElementByWeightWithFallback((RuleEntry rule) => (rule.knownUnresolvable || !rule.ValidateConstantConstraints(constants) || !rule.ValidateRequiredTag(extraTags, resolvedTags) || rule.Priority != maxPriority) ? 0f : rule.SelectionWeight);
        }





    }
}
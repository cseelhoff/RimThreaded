using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using RimWorld.Planet;

namespace RimThreaded
{

    public class GrammarResolverSimple_Patch
	{
        public static bool Formatted(ref TaggedString __result, TaggedString str, List<string> argsLabelsArg, List<object> argsObjectsArg)
        {
            if (str.NullOrEmpty())
            {
                __result = str;
                return false;
            }

            StringBuilder stringBuilder;
            StringBuilder stringBuilder2;
            StringBuilder stringBuilder3;
            StringBuilder stringBuilder4;
            StringBuilder stringBuilder5;
            List<string> list;
            List<object> list2;

            stringBuilder = new StringBuilder();
            stringBuilder2 = new StringBuilder();
            stringBuilder3 = new StringBuilder();
            stringBuilder4 = new StringBuilder();
            stringBuilder5 = new StringBuilder();
            list = argsLabelsArg.ToList();
            list2 = argsObjectsArg.ToList();

            stringBuilder.Length = 0;
            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];
                if (c == '{')
                {
                    stringBuilder2.Length = 0;
                    stringBuilder3.Length = 0;
                    stringBuilder4.Length = 0;
                    stringBuilder5.Length = 0;
                    bool flag2 = false;
                    bool flag3 = false;
                    bool flag4 = false;
                    i++;
                    bool flag5 = i < str.Length && str[i] == '{';
                    for (; i < str.Length; i++)
                    {
                        char c2 = str[i];
                        if (c2 == '}')
                        {
                            flag2 = true;
                            break;
                        }

                        stringBuilder2.Append(c2);
                        if (c2 == '_' && !flag3)
                        {
                            flag3 = true;
                        }
                        else if (c2 == '?' && !flag4)
                        {
                            flag4 = true;
                        }
                        else if (flag4)
                        {
                            stringBuilder5.Append(c2);
                        }
                        else if (flag3)
                        {
                            stringBuilder4.Append(c2);
                        }
                        else
                        {
                            stringBuilder3.Append(c2);
                        }
                    }

                    if (!flag2)
                    {
                        Log.ErrorOnce("Could not find matching '}' in \"" + str + "\".", str.GetHashCode() ^ 0xB9D492D);
                    }
                    else if (flag5)
                    {
                        stringBuilder.Append(stringBuilder2);
                    }
                    else
                    {
                        if (flag4)
                        {
                            while (stringBuilder4.Length != 0 && stringBuilder4[stringBuilder4.Length - 1] == ' ')
                            {
                                stringBuilder4.Length--;
                            }
                        }

                        string text = stringBuilder3.ToString();
                        bool flag6 = false;
                        int result = -1;
                        if (int.TryParse(text, out result))
                        {
                            if (result >= 0 && result < list2.Count && TryResolveSymbol(list2[result], stringBuilder4.ToString(), stringBuilder5.ToString(), out TaggedString resolvedStr, str))
                            {
                                flag6 = true;
                                stringBuilder.Append(resolvedStr.RawText);
                            }
                        }
                        else
                        {
                            for (int j = 0; j < list.Count; j++)
                            {
                                if (list[j] == text)
                                {
                                    if (TryResolveSymbol(list2[j], stringBuilder4.ToString(), stringBuilder5.ToString(), out TaggedString resolvedStr2, str))
                                    {
                                        flag6 = true;
                                        stringBuilder.Append(resolvedStr2.RawText);
                                    }

                                    break;
                                }
                            }
                        }

                        if (!flag6)
                        {
                            Log.ErrorOnce("Could not resolve symbol \"" + stringBuilder2 + "\" for string \"" + str + "\".", str.GetHashCode() ^ stringBuilder2.ToString().GetHashCode() ^ 0x346E76FE);
                        }
                    }
                }
                else
                {
                    stringBuilder.Append(c);
                }
            }

            string translation = GenText.CapitalizeSentences(stringBuilder.ToString(), capitalizeFirstSentence: false);
            translation = Find.ActiveLanguageWorker.PostProcessedKeyedTranslation(translation);
            __result = translation;
            return false;            
        }

        private static string ResolveMultipleSymbol(int count, string args, string fullStringForReference)
        {
            if (GetArgsCount(args) == 2)
            {
                if (count > 1)
                {
                    return GetArg(args, 0);
                }

                return GetArg(args, 1);
            }

            Log.ErrorOnce("Invalid args count in \"" + fullStringForReference + "\" for symbol \"multiple\".", args.GetHashCode() ^ fullStringForReference.GetHashCode() ^ 0xDC89D8D);
            return "";
        }



        private static void EnsureNoArgs(string subSymbol, string symbolArgs, string fullStringForReference)
        {
            if (!symbolArgs.NullOrEmpty())
            {
                Log.ErrorOnce("Symbol \"" + subSymbol + "\" doesn't expect any args but \"" + symbolArgs + "\" args were provided. Full string: \"" + fullStringForReference + "\".", subSymbol.GetHashCode() ^ symbolArgs.GetHashCode() ^ fullStringForReference.GetHashCode() ^ 0x391B4B8E);
            }
        }
        private static int GetArgsCount(string args)
        {
            int num = 1;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == ':')
                {
                    num++;
                }
            }

            return num;
        }

        private static string GetArg(string args, int argIndex)
        {
            StringBuilder tmpArg = new StringBuilder();
            tmpArg.Length = 0;
            int num = 0;
            foreach (char c in args)
            {
                if (c == ':')
                {
                    num++;
                }
                else if (num == argIndex)
                {
                    tmpArg.Append(c);
                }
                else if (num > argIndex)
                {
                    break;
                }
            }

            while (tmpArg.Length != 0 && tmpArg[0] == ' ')
            {
                tmpArg.Remove(0, 1);
            }

            while (tmpArg.Length != 0 && tmpArg[tmpArg.Length - 1] == ' ')
            {
                tmpArg.Length--;
            }

            return tmpArg.ToString();
        }


        private static string ResolveGenderSymbol(Gender gender, bool animal, string args, string fullStringForReference)
        {
            if (args.NullOrEmpty())
            {
                return gender.GetLabel(animal);
            }

            switch (GetArgsCount(args))
            {
                case 2:
                    switch (gender)
                    {
                        case Gender.Male:
                            return GetArg(args, 0);
                        case Gender.Female:
                            return GetArg(args, 1);
                        case Gender.None:
                            return GetArg(args, 0);
                        default:
                            return "";
                    }
                case 3:
                    switch (gender)
                    {
                        case Gender.Male:
                            return GetArg(args, 0);
                        case Gender.Female:
                            return GetArg(args, 1);
                        case Gender.None:
                            return GetArg(args, 2);
                        default:
                            return "";
                    }
                default:
                    Log.ErrorOnce("Invalid args count in \"" + fullStringForReference + "\" for symbol \"gender\".", args.GetHashCode() ^ fullStringForReference.GetHashCode() ^ 0x2EF21A43);
                    return "";
            }
        }

        private static string ResolveHumanlikeSymbol(bool humanlike, string args, string fullStringForReference)
        {
            if (GetArgsCount(args) == 2)
            {
                if (humanlike)
                {
                    return GetArg(args, 0);
                }

                return GetArg(args, 1);
            }

            Log.ErrorOnce("Invalid args count in \"" + fullStringForReference + "\" for symbol \"humanlike\".", args.GetHashCode() ^ fullStringForReference.GetHashCode() ^ 0x355A4AD5);
            return "";
        }


        private static bool TryResolveSymbol(object obj, string subSymbol, string symbolArgs, out TaggedString resolvedStr, string fullStringForReference)
        {
            Pawn pawn = obj as Pawn;
            switch (subSymbol)
            {
                case "":
                    resolvedStr = ((pawn.Name != null) ? Find.ActiveLanguageWorker.WithIndefiniteArticle(pawn.Name.ToStringShort, pawn.gender, plural: false, name: true).ApplyTag(TagType.Name) : ((TaggedString)pawn.KindLabelIndefinite()));
                    EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                    return true;
                case "nameFull":
                    resolvedStr = ((pawn.Name != null) ? Find.ActiveLanguageWorker.WithIndefiniteArticle(pawn.Name.ToStringFull, pawn.gender, plural: false, name: true).ApplyTag(TagType.Name) : ((TaggedString)pawn.KindLabelIndefinite()));
                    EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                    return true;
                case "nameFullDef":
                    resolvedStr = ((pawn.Name != null) ? Find.ActiveLanguageWorker.WithDefiniteArticle(pawn.Name.ToStringFull, pawn.gender, plural: false, name: true).ApplyTag(TagType.Name) : ((TaggedString)pawn.KindLabelDefinite()));
                    EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                    return true;
                case "label":
                    resolvedStr = pawn.LabelNoCountColored;
                    EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                    return true;
                case "labelShort":
                    resolvedStr = ((pawn.Name != null) ? pawn.Name.ToStringShort.ApplyTag(TagType.Name) : ((TaggedString)pawn.KindLabel));
                    EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                    return true;
                case "definite":
                    resolvedStr = ((pawn.Name != null) ? Find.ActiveLanguageWorker.WithDefiniteArticle(pawn.Name.ToStringShort, pawn.gender, plural: false, name: true).ApplyTag(TagType.Name) : ((TaggedString)pawn.KindLabelDefinite()));
                    EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                    return true;
                case "nameDef":
                    resolvedStr = ((pawn.Name != null) ? Find.ActiveLanguageWorker.WithDefiniteArticle(pawn.Name.ToStringShort, pawn.gender, plural: false, name: true).ApplyTag(TagType.Name) : ((TaggedString)pawn.KindLabelDefinite()));
                    EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                    return true;
                case "indefinite":
                    resolvedStr = ((pawn.Name != null) ? Find.ActiveLanguageWorker.WithIndefiniteArticle(pawn.Name.ToStringShort, pawn.gender, plural: false, name: true).ApplyTag(TagType.Name) : ((TaggedString)pawn.KindLabelIndefinite()));
                    EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                    return true;
                case "nameIndef":
                    resolvedStr = ((pawn.Name != null) ? Find.ActiveLanguageWorker.WithIndefiniteArticle(pawn.Name.ToStringShort, pawn.gender, plural: false, name: true).ApplyTag(TagType.Name) : ((TaggedString)pawn.KindLabelIndefinite()));
                    EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                    return true;
                case "pronoun":
                    resolvedStr = pawn.gender.GetPronoun();
                    EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                    return true;
                case "possessive":
                    resolvedStr = pawn.gender.GetPossessive();
                    EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                    return true;
                case "objective":
                    resolvedStr = pawn.gender.GetObjective();
                    EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                    return true;
                case "factionName":
                    resolvedStr = ((pawn.Faction != null) ? pawn.Faction.Name.ApplyTag(pawn.Faction) : ((TaggedString)""));
                    EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                    return true;
                case "factionPawnSingular":
                    resolvedStr = ((pawn.Faction != null) ? pawn.Faction.def.pawnSingular : "");
                    EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                    return true;
                case "factionPawnSingularDef":
                    resolvedStr = ((pawn.Faction != null) ? Find.ActiveLanguageWorker.WithDefiniteArticle(pawn.Faction.def.pawnSingular) : "");
                    EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                    return true;
                case "factionPawnSingularIndef":
                    resolvedStr = ((pawn.Faction != null) ? Find.ActiveLanguageWorker.WithIndefiniteArticle(pawn.Faction.def.pawnSingular) : "");
                    EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                    return true;
                case "factionPawnsPlural":
                    resolvedStr = ((pawn.Faction != null) ? pawn.Faction.def.pawnsPlural : "");
                    EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                    return true;
                case "factionPawnsPluralDef":
                    resolvedStr = ((pawn.Faction != null) ? Find.ActiveLanguageWorker.WithDefiniteArticle(pawn.Faction.def.pawnsPlural, LanguageDatabase.activeLanguage.ResolveGender(pawn.Faction.def.pawnsPlural, pawn.Faction.def.pawnSingular), plural: true) : "");
                    EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                    return true;
                case "factionPawnsPluralIndef":
                    resolvedStr = ((pawn.Faction != null) ? Find.ActiveLanguageWorker.WithIndefiniteArticle(pawn.Faction.def.pawnsPlural, LanguageDatabase.activeLanguage.ResolveGender(pawn.Faction.def.pawnsPlural, pawn.Faction.def.pawnSingular), plural: true) : "");
                    EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                    return true;
                case "factionRoyalFavorLabel":
                    resolvedStr = ((pawn.Faction != null) ? pawn.Faction.def.royalFavorLabel : "");
                    EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                    return true;
                case "kind":
                    resolvedStr = pawn.KindLabel;
                    EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                    return true;
                case "kindDef":
                    resolvedStr = pawn.KindLabelDefinite();
                    EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                    return true;
                case "kindIndef":
                    resolvedStr = pawn.KindLabelIndefinite();
                    EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                    return true;
                case "kindPlural":
                    resolvedStr = pawn.GetKindLabelPlural();
                    EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                    return true;
                case "kindPluralDef":
                    resolvedStr = Find.ActiveLanguageWorker.WithDefiniteArticle(pawn.GetKindLabelPlural(), pawn.gender, plural: true);
                    EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                    return true;
                case "kindPluralIndef":
                    resolvedStr = Find.ActiveLanguageWorker.WithIndefiniteArticle(pawn.GetKindLabelPlural(), pawn.gender, plural: true);
                    EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                    return true;
                case "kindBase":
                    resolvedStr = pawn.kindDef.label;
                    EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                    return true;
                case "kindBaseDef":
                    resolvedStr = Find.ActiveLanguageWorker.WithDefiniteArticle(pawn.kindDef.label);
                    EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                    return true;
                case "kindBaseIndef":
                    resolvedStr = Find.ActiveLanguageWorker.WithIndefiniteArticle(pawn.kindDef.label);
                    EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                    return true;
                case "kindBasePlural":
                    resolvedStr = pawn.kindDef.GetLabelPlural();
                    EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                    return true;
                case "kindBasePluralDef":
                    resolvedStr = Find.ActiveLanguageWorker.WithDefiniteArticle(pawn.kindDef.GetLabelPlural(), LanguageDatabase.activeLanguage.ResolveGender(pawn.kindDef.GetLabelPlural(), pawn.kindDef.label), plural: true);
                    EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                    return true;
                case "kindBasePluralIndef":
                    resolvedStr = Find.ActiveLanguageWorker.WithIndefiniteArticle(pawn.kindDef.GetLabelPlural(), LanguageDatabase.activeLanguage.ResolveGender(pawn.kindDef.GetLabelPlural(), pawn.kindDef.label), plural: true);
                    EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                    return true;
                case "race":
                    resolvedStr = pawn.def.label;
                    EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                    return true;
                case "raceDef":
                    resolvedStr = Find.ActiveLanguageWorker.WithDefiniteArticle(pawn.def.label);
                    EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                    return true;
                case "raceIndef":
                    resolvedStr = Find.ActiveLanguageWorker.WithIndefiniteArticle(pawn.def.label);
                    EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                    return true;
                case "lifeStage":
                    resolvedStr = pawn.ageTracker.CurLifeStage.label;
                    EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                    return true;
                case "lifeStageDef":
                    resolvedStr = Find.ActiveLanguageWorker.WithDefiniteArticle(pawn.ageTracker.CurLifeStage.label, pawn.gender);
                    EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                    return true;
                case "lifeStageIndef":
                    resolvedStr = Find.ActiveLanguageWorker.WithIndefiniteArticle(pawn.ageTracker.CurLifeStage.label, pawn.gender);
                    EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                    return true;
                case "lifeStageAdjective":
                    resolvedStr = pawn.ageTracker.CurLifeStage.Adjective;
                    EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                    return true;
                case "title":
                    resolvedStr = ((pawn.story != null) ? pawn.story.Title : "");
                    EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                    return true;
                case "titleDef":
                    resolvedStr = ((pawn.story != null) ? Find.ActiveLanguageWorker.WithDefiniteArticle(pawn.story.Title, pawn.gender) : "");
                    EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                    return true;
                case "titleIndef":
                    resolvedStr = ((pawn.story != null) ? Find.ActiveLanguageWorker.WithIndefiniteArticle(pawn.story.Title, pawn.gender) : "");
                    EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                    return true;
                case "bestRoyalTitle":
                    resolvedStr = GrammarResolverSimple.PawnResolveBestRoyalTitle(pawn);
                    EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                    return true;
                case "bestRoyalTitleIndef":
                    resolvedStr = Find.ActiveLanguageWorker.WithIndefiniteArticle(GrammarResolverSimple.PawnResolveBestRoyalTitle(pawn));
                    EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                    return true;
                case "bestRoyalTitleDef":
                    resolvedStr = Find.ActiveLanguageWorker.WithDefiniteArticle(GrammarResolverSimple.PawnResolveBestRoyalTitle(pawn));
                    EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                    return true;
                case "royalTitleInCurrentFaction":
                    resolvedStr = GrammarResolverSimple.PawnResolveRoyalTitleInCurrentFaction(pawn);
                    EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                    return true;
                case "royalTitleInCurrentFactionIndef":
                    resolvedStr = Find.ActiveLanguageWorker.WithIndefiniteArticle(GrammarResolverSimple.PawnResolveRoyalTitleInCurrentFaction(pawn));
                    EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                    return true;
                case "royalTitleInCurrentFactionDef":
                    resolvedStr = Find.ActiveLanguageWorker.WithDefiniteArticle(GrammarResolverSimple.PawnResolveRoyalTitleInCurrentFaction(pawn));
                    EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                    return true;
                case "age":
                    resolvedStr = pawn.ageTracker.AgeBiologicalYears.ToString();
                    EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                    return true;
                case "chronologicalAge":
                    resolvedStr = pawn.ageTracker.AgeChronologicalYears.ToString();
                    EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                    return true;
                case "ageFull":
                    resolvedStr = pawn.ageTracker.AgeNumberString;
                    EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                    return true;
                case "relationInfo":
                    {
                        resolvedStr = "";
                        TaggedString text2 = resolvedStr;
                        PawnRelationUtility.TryAppendRelationsWithColonistsInfo(ref text2, pawn);
                        resolvedStr = text2.RawText;
                        return true;
                    }
                case "relationInfoInParentheses":
                    resolvedStr = "";
                    PawnRelationUtility.TryAppendRelationsWithColonistsInfo(ref resolvedStr, pawn);
                    if (!resolvedStr.NullOrEmpty())
                    {
                        resolvedStr = "(" + resolvedStr + ")";
                    }

                    return true;
                case "gender":
                    resolvedStr = ResolveGenderSymbol(pawn.gender, pawn.RaceProps.Animal, symbolArgs, fullStringForReference);
                    return true;
                case "humanlike":
                    resolvedStr = ResolveHumanlikeSymbol(pawn.RaceProps.Humanlike, symbolArgs, fullStringForReference);
                    return true;
                default:
                    resolvedStr = "";
                    return false;
                case null:
                    {
                        Thing thing = obj as Thing;
                        switch (subSymbol)
                        {
                            case "":
                                resolvedStr = Find.ActiveLanguageWorker.WithIndefiniteArticle(thing.Label);
                                EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                return true;
                            case "label":
                                resolvedStr = thing.Label;
                                EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                return true;
                            case "labelPlural":
                                resolvedStr = Find.ActiveLanguageWorker.Pluralize(thing.LabelNoCount);
                                EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                return true;
                            case "labelPluralDef":
                                resolvedStr = Find.ActiveLanguageWorker.WithDefiniteArticle(Find.ActiveLanguageWorker.Pluralize(thing.LabelNoCount), LanguageDatabase.activeLanguage.ResolveGender(Find.ActiveLanguageWorker.Pluralize(thing.LabelNoCount), thing.LabelNoCount), plural: true);
                                EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                return true;
                            case "labelPluralIndef":
                                resolvedStr = Find.ActiveLanguageWorker.WithIndefiniteArticle(Find.ActiveLanguageWorker.Pluralize(thing.LabelNoCount), LanguageDatabase.activeLanguage.ResolveGender(Find.ActiveLanguageWorker.Pluralize(thing.LabelNoCount), thing.LabelNoCount), plural: true);
                                EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                return true;
                            case "labelShort":
                                resolvedStr = thing.LabelShort;
                                EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                return true;
                            case "definite":
                                resolvedStr = Find.ActiveLanguageWorker.WithDefiniteArticle(thing.Label);
                                EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                return true;
                            case "indefinite":
                                resolvedStr = Find.ActiveLanguageWorker.WithIndefiniteArticle(thing.Label);
                                EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                return true;
                            case "pronoun":
                                resolvedStr = LanguageDatabase.activeLanguage.ResolveGender(thing.LabelNoCount).GetPronoun();
                                EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                return true;
                            case "possessive":
                                resolvedStr = LanguageDatabase.activeLanguage.ResolveGender(thing.LabelNoCount).GetPossessive();
                                EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                return true;
                            case "objective":
                                resolvedStr = LanguageDatabase.activeLanguage.ResolveGender(thing.LabelNoCount).GetObjective();
                                EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                return true;
                            case "factionName":
                                resolvedStr = ((thing.Faction != null) ? thing.Faction.Name : "");
                                EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                return true;
                            case "gender":
                                resolvedStr = ResolveGenderSymbol(LanguageDatabase.activeLanguage.ResolveGender(thing.LabelNoCount), animal: false, symbolArgs, fullStringForReference);
                                return true;
                            default:
                                resolvedStr = "";
                                return false;
                            case null:
                                {
                                    Hediff hediff = obj as Hediff;
                                    if (hediff != null)
                                    {
                                        if (subSymbol == "label")
                                        {
                                            resolvedStr = hediff.Label;
                                            EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                            return true;
                                        }

                                        if (subSymbol == "labelNoun")
                                        {
                                            resolvedStr = ((!hediff.def.labelNoun.NullOrEmpty()) ? hediff.def.labelNoun : hediff.Label);
                                            EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                            return true;
                                        }
                                    }

                                    WorldObject worldObject = obj as WorldObject;
                                    switch (subSymbol)
                                    {
                                        case "":
                                            resolvedStr = Find.ActiveLanguageWorker.WithIndefiniteArticle(worldObject.Label, plural: false, worldObject.HasName);
                                            EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                            return true;
                                        case "label":
                                            resolvedStr = worldObject.Label;
                                            EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                            return true;
                                        case "labelPlural":
                                            resolvedStr = Find.ActiveLanguageWorker.Pluralize(worldObject.Label);
                                            EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                            return true;
                                        case "labelPluralDef":
                                            resolvedStr = Find.ActiveLanguageWorker.WithDefiniteArticle(Find.ActiveLanguageWorker.Pluralize(worldObject.Label), LanguageDatabase.activeLanguage.ResolveGender(Find.ActiveLanguageWorker.Pluralize(worldObject.Label), worldObject.Label), plural: true, worldObject.HasName);
                                            EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                            return true;
                                        case "labelPluralIndef":
                                            resolvedStr = Find.ActiveLanguageWorker.WithIndefiniteArticle(Find.ActiveLanguageWorker.Pluralize(worldObject.Label), LanguageDatabase.activeLanguage.ResolveGender(Find.ActiveLanguageWorker.Pluralize(worldObject.Label), worldObject.Label), plural: true, worldObject.HasName);
                                            EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                            return true;
                                        case "definite":
                                            resolvedStr = Find.ActiveLanguageWorker.WithDefiniteArticle(worldObject.Label, plural: false, worldObject.HasName);
                                            EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                            return true;
                                        case "indefinite":
                                            resolvedStr = Find.ActiveLanguageWorker.WithIndefiniteArticle(worldObject.Label, plural: false, worldObject.HasName);
                                            EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                            return true;
                                        case "pronoun":
                                            resolvedStr = LanguageDatabase.activeLanguage.ResolveGender(worldObject.Label).GetPronoun();
                                            EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                            return true;
                                        case "possessive":
                                            resolvedStr = LanguageDatabase.activeLanguage.ResolveGender(worldObject.Label).GetPossessive();
                                            EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                            return true;
                                        case "objective":
                                            resolvedStr = LanguageDatabase.activeLanguage.ResolveGender(worldObject.Label).GetObjective();
                                            EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                            return true;
                                        case "factionName":
                                            resolvedStr = ((worldObject.Faction != null) ? worldObject.Faction.Name.ApplyTag(worldObject.Faction) : ((TaggedString)""));
                                            EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                            return true;
                                        case "gender":
                                            resolvedStr = ResolveGenderSymbol(LanguageDatabase.activeLanguage.ResolveGender(worldObject.Label), animal: false, symbolArgs, fullStringForReference);
                                            return true;
                                        default:
                                            resolvedStr = "";
                                            return false;
                                        case null:
                                            {
                                                Faction faction = obj as Faction;
                                                switch (subSymbol)
                                                {
                                                    case "":
                                                        resolvedStr = faction.Name.ApplyTag(faction);
                                                        EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                                        return true;
                                                    case "name":
                                                        resolvedStr = faction.Name.ApplyTag(faction);
                                                        EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                                        return true;
                                                    case "pawnSingular":
                                                        resolvedStr = faction.def.pawnSingular;
                                                        EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                                        return true;
                                                    case "pawnSingularDef":
                                                        resolvedStr = Find.ActiveLanguageWorker.WithDefiniteArticle(faction.def.pawnSingular);
                                                        EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                                        return true;
                                                    case "pawnSingularIndef":
                                                        resolvedStr = Find.ActiveLanguageWorker.WithIndefiniteArticle(faction.def.pawnSingular);
                                                        EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                                        return true;
                                                    case "pawnsPlural":
                                                        resolvedStr = faction.def.pawnsPlural;
                                                        EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                                        return true;
                                                    case "pawnsPluralDef":
                                                        resolvedStr = Find.ActiveLanguageWorker.WithDefiniteArticle(faction.def.pawnsPlural, LanguageDatabase.activeLanguage.ResolveGender(faction.def.pawnsPlural, faction.def.pawnSingular), plural: true);
                                                        EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                                        return true;
                                                    case "pawnsPluralIndef":
                                                        resolvedStr = Find.ActiveLanguageWorker.WithIndefiniteArticle(faction.def.pawnsPlural, LanguageDatabase.activeLanguage.ResolveGender(faction.def.pawnsPlural, faction.def.pawnSingular), plural: true);
                                                        EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                                        return true;
                                                    case "royalFavorLabel":
                                                        resolvedStr = faction.def.royalFavorLabel;
                                                        EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                                        return true;
                                                    case "leaderNameDef":
                                                        resolvedStr = ((faction.leader != null && faction.leader.Name != null) ? Find.ActiveLanguageWorker.WithDefiniteArticle(faction.leader.Name.ToStringShort, faction.leader.gender, plural: false, name: true).ApplyTag(TagType.Name) : ((TaggedString)""));
                                                        EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                                        return true;
                                                    case "leaderPossessive":
                                                        resolvedStr = ((faction.leader != null) ? faction.leader.gender.GetPossessive() : "");
                                                        EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                                        return true;
                                                    case "leaderObjective":
                                                        resolvedStr = ((faction.leader != null) ? faction.leader.gender.GetObjective() : "");
                                                        EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                                        return true;
                                                    case "leaderPronoun":
                                                        resolvedStr = ((faction.leader != null) ? faction.leader.gender.GetPronoun() : "");
                                                        EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                                        return true;
                                                    default:
                                                        resolvedStr = "";
                                                        return false;
                                                    case null:
                                                        {
                                                            Def def = obj as Def;
                                                            if (def != null)
                                                            {
                                                                PawnKindDef pawnKindDef = def as PawnKindDef;
                                                                if (pawnKindDef != null)
                                                                {
                                                                    if (subSymbol == "labelPlural")
                                                                    {
                                                                        resolvedStr = pawnKindDef.GetLabelPlural();
                                                                        EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                                                        return true;
                                                                    }

                                                                    if (subSymbol == "labelPluralDef")
                                                                    {
                                                                        resolvedStr = Find.ActiveLanguageWorker.WithDefiniteArticle(pawnKindDef.GetLabelPlural(), LanguageDatabase.activeLanguage.ResolveGender(pawnKindDef.GetLabelPlural(), pawnKindDef.label), plural: true);
                                                                        EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                                                        return true;
                                                                    }

                                                                    if (subSymbol == "labelPluralIndef")
                                                                    {
                                                                        resolvedStr = Find.ActiveLanguageWorker.WithIndefiniteArticle(pawnKindDef.GetLabelPlural(), LanguageDatabase.activeLanguage.ResolveGender(pawnKindDef.GetLabelPlural(), pawnKindDef.label), plural: true);
                                                                        EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                                                        return true;
                                                                    }
                                                                }

                                                                switch (subSymbol)
                                                                {
                                                                    case "":
                                                                        resolvedStr = Find.ActiveLanguageWorker.WithIndefiniteArticle(def.label);
                                                                        EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                                                        return true;
                                                                    case "label":
                                                                        resolvedStr = def.label;
                                                                        EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                                                        return true;
                                                                    case "labelPlural":
                                                                        resolvedStr = Find.ActiveLanguageWorker.Pluralize(def.label);
                                                                        EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                                                        return true;
                                                                    case "labelPluralDef":
                                                                        resolvedStr = Find.ActiveLanguageWorker.WithDefiniteArticle(Find.ActiveLanguageWorker.Pluralize(def.label), LanguageDatabase.activeLanguage.ResolveGender(Find.ActiveLanguageWorker.Pluralize(def.label), def.label), plural: true);
                                                                        EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                                                        return true;
                                                                    case "labelPluralIndef":
                                                                        resolvedStr = Find.ActiveLanguageWorker.WithIndefiniteArticle(Find.ActiveLanguageWorker.Pluralize(def.label), LanguageDatabase.activeLanguage.ResolveGender(Find.ActiveLanguageWorker.Pluralize(def.label), def.label), plural: true);
                                                                        EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                                                        return true;
                                                                    case "definite":
                                                                        resolvedStr = Find.ActiveLanguageWorker.WithDefiniteArticle(def.label);
                                                                        EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                                                        return true;
                                                                    case "indefinite":
                                                                        resolvedStr = Find.ActiveLanguageWorker.WithIndefiniteArticle(def.label);
                                                                        EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                                                        return true;
                                                                    case "pronoun":
                                                                        resolvedStr = LanguageDatabase.activeLanguage.ResolveGender(def.label).GetPronoun();
                                                                        EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                                                        return true;
                                                                    case "possessive":
                                                                        resolvedStr = LanguageDatabase.activeLanguage.ResolveGender(def.label).GetPossessive();
                                                                        EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                                                        return true;
                                                                    case "objective":
                                                                        resolvedStr = LanguageDatabase.activeLanguage.ResolveGender(def.label).GetObjective();
                                                                        EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                                                        return true;
                                                                    case "gender":
                                                                        resolvedStr = ResolveGenderSymbol(LanguageDatabase.activeLanguage.ResolveGender(def.label), animal: false, symbolArgs, fullStringForReference);
                                                                        return true;
                                                                    default:
                                                                        resolvedStr = "";
                                                                        return false;
                                                                }
                                                            }

                                                            RoyalTitle royalTitle = obj as RoyalTitle;
                                                            if (royalTitle != null)
                                                            {
                                                                if (subSymbol == null || subSymbol.Length != 0)
                                                                {
                                                                    if (!(subSymbol == "label"))
                                                                    {
                                                                        if (subSymbol == "indefinite")
                                                                        {
                                                                            resolvedStr = Find.ActiveLanguageWorker.WithIndefiniteArticlePostProcessed(royalTitle.Label);
                                                                            EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                                                            return true;
                                                                        }

                                                                        resolvedStr = "";
                                                                        return false;
                                                                    }

                                                                    resolvedStr = royalTitle.Label;
                                                                    EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                                                    return true;
                                                                }

                                                                resolvedStr = royalTitle.Label;
                                                                EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                                                return true;
                                                            }

                                                            string text = obj as string;
                                                            switch (subSymbol)
                                                            {
                                                                case "":
                                                                    resolvedStr = text;
                                                                    EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                                                    return true;
                                                                case "plural":
                                                                    resolvedStr = Find.ActiveLanguageWorker.Pluralize(text);
                                                                    EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                                                    return true;
                                                                case "pluralDef":
                                                                    resolvedStr = Find.ActiveLanguageWorker.WithDefiniteArticle(Find.ActiveLanguageWorker.Pluralize(text), LanguageDatabase.activeLanguage.ResolveGender(Find.ActiveLanguageWorker.Pluralize(text), text), plural: true);
                                                                    EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                                                    return true;
                                                                case "pluralIndef":
                                                                    resolvedStr = Find.ActiveLanguageWorker.WithIndefiniteArticle(Find.ActiveLanguageWorker.Pluralize(text), LanguageDatabase.activeLanguage.ResolveGender(Find.ActiveLanguageWorker.Pluralize(text), text), plural: true);
                                                                    EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                                                    return true;
                                                                case "definite":
                                                                    resolvedStr = Find.ActiveLanguageWorker.WithDefiniteArticle(text);
                                                                    EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                                                    return true;
                                                                case "indefinite":
                                                                    resolvedStr = Find.ActiveLanguageWorker.WithIndefiniteArticle(text);
                                                                    EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                                                    return true;
                                                                case "pronoun":
                                                                    resolvedStr = LanguageDatabase.activeLanguage.ResolveGender(text).GetPronoun();
                                                                    EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                                                    return true;
                                                                case "possessive":
                                                                    resolvedStr = LanguageDatabase.activeLanguage.ResolveGender(text).GetPossessive();
                                                                    EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                                                    return true;
                                                                case "objective":
                                                                    resolvedStr = LanguageDatabase.activeLanguage.ResolveGender(text).GetObjective();
                                                                    EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                                                    return true;
                                                                case "gender":
                                                                    resolvedStr = ResolveGenderSymbol(LanguageDatabase.activeLanguage.ResolveGender(text), animal: false, symbolArgs, fullStringForReference);
                                                                    return true;
                                                                default:
                                                                    resolvedStr = "";
                                                                    return false;
                                                                case null:
                                                                    if (obj is int || obj is long)
                                                                    {
                                                                        int num = (int)((obj is int) ? ((int)obj) : ((long)obj));
                                                                        if (subSymbol == null || subSymbol.Length != 0)
                                                                        {
                                                                            if (!(subSymbol == "ordinal"))
                                                                            {
                                                                                if (subSymbol == "multiple")
                                                                                {
                                                                                    resolvedStr = ResolveMultipleSymbol(num, symbolArgs, fullStringForReference);
                                                                                    return true;
                                                                                }

                                                                                resolvedStr = "";
                                                                                return false;
                                                                            }

                                                                            resolvedStr = Find.ActiveLanguageWorker.OrdinalNumber(num).ToString();
                                                                            EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                                                            return true;
                                                                        }

                                                                        resolvedStr = num.ToString();
                                                                        EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                                                        return true;
                                                                    }

                                                                    if (obj is TaggedString)
                                                                    {
                                                                        EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                                                        resolvedStr = ((TaggedString)obj).RawText;
                                                                    }

                                                                    if (subSymbol.NullOrEmpty())
                                                                    {
                                                                        EnsureNoArgs(subSymbol, symbolArgs, fullStringForReference);
                                                                        if (obj == null)
                                                                        {
                                                                            resolvedStr = "";
                                                                        }
                                                                        else
                                                                        {
                                                                            resolvedStr = obj.ToString();
                                                                        }

                                                                        return true;
                                                                    }

                                                                    resolvedStr = "";
                                                                    return false;
                                                            }
                                                        }
                                                }
                                            }
                                    }
                                }
                        }
                    }
            }
        }



    }
}

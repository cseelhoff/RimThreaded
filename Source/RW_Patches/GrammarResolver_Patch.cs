using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;
using Verse.Grammar;
using static HarmonyLib.AccessTools;

namespace RimThreaded.RW_Patches
{

    public class GrammarResolver_Patch
    {
        private static readonly object ResolveUnsafeLock = new object();
        private static readonly object ResolveLock = new object();

        //[ThreadStatic] public static Dictionary<string, List<RuleEntry>> rules = new Dictionary<string, List<RuleEntry>>();

        public static Type original = typeof(GrammarResolver);
        public static Type patched = typeof(GrammarResolver_Patch);

        //internal static void InitializeThreadStatics()
        //      {
        //	rules = new Dictionary<string, List<RuleEntry>>();
        //}

        internal static void RunNonDestructivePatches()
        {
            //RimThreadedHarmony.Transpile(original, patched, nameof(ResolveUnsafe), new Type[] {
            //	typeof(string), typeof(GrammarRequest), typeof(bool).MakeByRefType(), typeof(string), typeof(bool), typeof(bool), typeof(List<string>), typeof(List<string>), typeof(bool)
            //});
            //RimThreadedHarmony.Transpile(original, patched, nameof(Resolve), new Type[] {
            //	typeof(string), typeof(GrammarRequest), typeof(string), typeof(bool), typeof(string), typeof(List<string>), typeof(List<string>), typeof(bool)
            //});
        }
        public static bool Resolve(ref string __result, string rootKeyword, GrammarRequest request, string debugLabel = null, bool forceLog = false, string untranslatedRootKeyword = null, List<string> extraTags = null, List<string> outTags = null, bool capitalizeFirstSentence = true)
        {
            lock (ResolveLock)
            {
                if (LanguageDatabase.activeLanguage == LanguageDatabase.defaultLanguage)
                {
                    __result = GrammarResolver.ResolveUnsafe(rootKeyword, request, debugLabel, forceLog, useUntranslatedRules: false, extraTags, outTags, capitalizeFirstSentence);
                    return false;
                }
                string text;
                bool success;
                Exception ex;
                try
                {
                    text = GrammarResolver.ResolveUnsafe(rootKeyword, request, out success, debugLabel, forceLog, useUntranslatedRules: false, extraTags, outTags, capitalizeFirstSentence);
                    ex = null;
                }
                catch (Exception ex2)
                {
                    success = false;
                    text = "";
                    ex = ex2;
                }
                if (success)
                {
                    __result = text;
                    return false;
                }
                string text2 = "Failed to resolve text. Trying again with English.";
                if (ex != null)
                {
                    text2 = text2 + " Exception: " + ex;
                }
                Log.ErrorOnce(text2, text.GetHashCode());
                outTags?.Clear();
                __result = GrammarResolver.ResolveUnsafe(untranslatedRootKeyword ?? rootKeyword, request, out success, debugLabel, forceLog, useUntranslatedRules: true, extraTags, outTags, capitalizeFirstSentence);
                return false;
            }
        }

        public static IEnumerable<CodeInstruction> ResolveUnsafe(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            List<CodeInstruction> instructionsList = instructions.ToList();
            int i = 0;
            List<CodeInstruction> loadLockObjectInstructions = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldsfld, Field(patched, nameof(ResolveUnsafeLock)))
            };
            LocalBuilder lockObject = iLGenerator.DeclareLocal(typeof(object));
            LocalBuilder lockTaken = iLGenerator.DeclareLocal(typeof(bool));
            foreach (CodeInstruction ci in RimThreadedHarmony.EnterLock(lockObject, lockTaken, loadLockObjectInstructions, instructionsList[i]))
                yield return ci;
            while (i < instructionsList.Count - 2)
            {
                yield return instructionsList[i++];
            }
            foreach (CodeInstruction ci in RimThreadedHarmony.ExitLock(iLGenerator, lockObject, lockTaken, instructionsList[i]))
                yield return ci;
            yield return instructionsList[i++];
            yield return instructionsList[i++];
        }

        public static IEnumerable<CodeInstruction> Resolve(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            List<CodeInstruction> instructionsList = instructions.ToList();
            int i = 0;
            List<CodeInstruction> loadLockObjectInstructions = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldsfld, Field(patched, nameof(ResolveLock)))
            };
            LocalBuilder lockObject = iLGenerator.DeclareLocal(typeof(object));
            LocalBuilder lockTaken = iLGenerator.DeclareLocal(typeof(bool));
            foreach (CodeInstruction ci in RimThreadedHarmony.EnterLock(lockObject, lockTaken, loadLockObjectInstructions, instructionsList[i]))
                yield return ci;
            while (i < instructionsList.Count - 1)
            {
                yield return instructionsList[i++];
            }
            LocalBuilder stringResult = iLGenerator.DeclareLocal(typeof(string));
            yield return new CodeInstruction(OpCodes.Stloc, stringResult);
            foreach (CodeInstruction ci in RimThreadedHarmony.ExitLock(iLGenerator, lockObject, lockTaken, instructionsList[i]))
                yield return ci;
            yield return new CodeInstruction(OpCodes.Ldloc, stringResult);
            yield return instructionsList[i++];
            //yield return instructionsList[i++];
        }

    }
}
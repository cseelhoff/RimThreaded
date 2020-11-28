using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using System.Threading;

namespace RimThreaded
{

    public class GrammarResolverSimpleStringExtensions_Patch
	{

        public static Dictionary<int, List<string>> argsLabelsDict = new Dictionary<int, List<string>>();
        public static Dictionary<int, List<object>> argsObjectsDict = new Dictionary<int, List<object>>();
        public static List<string> getArgsLabels()
        {
            int tID = Thread.CurrentThread.ManagedThreadId;
            if (!argsLabelsDict.TryGetValue(tID, out List<string> argsLabels))
            {
                argsLabels = new List<string>();
                argsLabelsDict[tID] = argsLabels;
            }
            return argsLabels;
        }
        public static List<object> getArgsObjects()
        {
            int tID = Thread.CurrentThread.ManagedThreadId;
            if (!argsObjectsDict.TryGetValue(tID, out List<object> argsObjects))
            {
                argsObjects = new List<object>();
                argsObjectsDict[tID] = argsObjects;
            }
            return argsObjects;
        }
        public static bool Formatted(ref TaggedString __result, string str, NamedArgument arg1)
        {
            List<string> argsLabels = getArgsLabels();
            List<object> argsObjects = getArgsObjects();
            argsLabels.Clear();
            argsObjects.Clear();
            argsLabels.Add(arg1.label);
            argsObjects.Add(arg1.arg);
            __result = GrammarResolverSimple.Formatted(str, argsLabels, argsObjects);
            return false;
        }

        public static bool Formatted(ref TaggedString __result, string str, NamedArgument arg1, NamedArgument arg2)
        {
            List<string> argsLabels = getArgsLabels();
            List<object> argsObjects = getArgsObjects();
            argsLabels.Clear();
            argsObjects.Clear();
            argsLabels.Add(arg1.label);
            argsObjects.Add(arg1.arg);
            argsLabels.Add(arg2.label);
            argsObjects.Add(arg2.arg);
            __result = GrammarResolverSimple.Formatted(str, argsLabels, argsObjects);
            return false;
        }

        public static bool Formatted(ref TaggedString __result, string str, NamedArgument arg1, NamedArgument arg2, NamedArgument arg3)
        {
            List<string> argsLabels = getArgsLabels();
            List<object> argsObjects = getArgsObjects();
            argsLabels.Clear();
            argsObjects.Clear();
            argsLabels.Add(arg1.label);
            argsObjects.Add(arg1.arg);
            argsLabels.Add(arg2.label);
            argsObjects.Add(arg2.arg);
            argsLabels.Add(arg3.label);
            argsObjects.Add(arg3.arg);
            __result = GrammarResolverSimple.Formatted(str, argsLabels, argsObjects);
            return false;
        }

        public static bool Formatted(ref TaggedString __result, string str, NamedArgument arg1, NamedArgument arg2, NamedArgument arg3, NamedArgument arg4)
        {
            List<string> argsLabels = getArgsLabels();
            List<object> argsObjects = getArgsObjects();

            argsLabels.Clear();
            argsObjects.Clear();
            argsLabels.Add(arg1.label);
            argsObjects.Add(arg1.arg);
            argsLabels.Add(arg2.label);
            argsObjects.Add(arg2.arg);
            argsLabels.Add(arg3.label);
            argsObjects.Add(arg3.arg);
            argsLabels.Add(arg4.label);
            argsObjects.Add(arg4.arg);
            __result = GrammarResolverSimple.Formatted(str, argsLabels, argsObjects);
            return false;
        }

        public static bool Formatted(ref TaggedString __result, string str, NamedArgument arg1, NamedArgument arg2, NamedArgument arg3, NamedArgument arg4, NamedArgument arg5)
        {
            List<string> argsLabels = getArgsLabels();
            List<object> argsObjects = getArgsObjects();

            argsLabels.Clear();
            argsObjects.Clear();
            argsLabels.Add(arg1.label);
            argsObjects.Add(arg1.arg);
            argsLabels.Add(arg2.label);
            argsObjects.Add(arg2.arg);
            argsLabels.Add(arg3.label);
            argsObjects.Add(arg3.arg);
            argsLabels.Add(arg4.label);
            argsObjects.Add(arg4.arg);
            argsLabels.Add(arg5.label);
            argsObjects.Add(arg5.arg);
            __result = GrammarResolverSimple.Formatted(str, argsLabels, argsObjects);
            return false;
        }

        public static bool Formatted(ref TaggedString __result, string str, NamedArgument arg1, NamedArgument arg2, NamedArgument arg3, NamedArgument arg4, NamedArgument arg5, NamedArgument arg6)
        {
            List<string> argsLabels = getArgsLabels();
            List<object> argsObjects = getArgsObjects();

            argsLabels.Clear();
            argsObjects.Clear();
            argsLabels.Add(arg1.label);
            argsObjects.Add(arg1.arg);
            argsLabels.Add(arg2.label);
            argsObjects.Add(arg2.arg);
            argsLabels.Add(arg3.label);
            argsObjects.Add(arg3.arg);
            argsLabels.Add(arg4.label);
            argsObjects.Add(arg4.arg);
            argsLabels.Add(arg5.label);
            argsObjects.Add(arg5.arg);
            argsLabels.Add(arg6.label);
            argsObjects.Add(arg6.arg);
            __result = GrammarResolverSimple.Formatted(str, argsLabels, argsObjects);
            return false;
        }

        public static bool Formatted(ref TaggedString __result, string str, NamedArgument arg1, NamedArgument arg2, NamedArgument arg3, NamedArgument arg4, NamedArgument arg5, NamedArgument arg6, NamedArgument arg7)
        {
            List<string> argsLabels = getArgsLabels();
            List<object> argsObjects = getArgsObjects();

            argsLabels.Clear();
            argsObjects.Clear();
            argsLabels.Add(arg1.label);
            argsObjects.Add(arg1.arg);
            argsLabels.Add(arg2.label);
            argsObjects.Add(arg2.arg);
            argsLabels.Add(arg3.label);
            argsObjects.Add(arg3.arg);
            argsLabels.Add(arg4.label);
            argsObjects.Add(arg4.arg);
            argsLabels.Add(arg5.label);
            argsObjects.Add(arg5.arg);
            argsLabels.Add(arg6.label);
            argsObjects.Add(arg6.arg);
            argsLabels.Add(arg7.label);
            argsObjects.Add(arg7.arg);
            __result = GrammarResolverSimple.Formatted(str, argsLabels, argsObjects);
            return false;
        }

        public static bool Formatted(ref TaggedString __result, string str, NamedArgument arg1, NamedArgument arg2, NamedArgument arg3, NamedArgument arg4, NamedArgument arg5, NamedArgument arg6, NamedArgument arg7, NamedArgument arg8)
        {
            List<string> argsLabels = getArgsLabels();
            List<object> argsObjects = getArgsObjects();

            argsLabels.Clear();
            argsObjects.Clear();
            argsLabels.Add(arg1.label);
            argsObjects.Add(arg1.arg);
            argsLabels.Add(arg2.label);
            argsObjects.Add(arg2.arg);
            argsLabels.Add(arg3.label);
            argsObjects.Add(arg3.arg);
            argsLabels.Add(arg4.label);
            argsObjects.Add(arg4.arg);
            argsLabels.Add(arg5.label);
            argsObjects.Add(arg5.arg);
            argsLabels.Add(arg6.label);
            argsObjects.Add(arg6.arg);
            argsLabels.Add(arg7.label);
            argsObjects.Add(arg7.arg);
            argsLabels.Add(arg8.label);
            argsObjects.Add(arg8.arg);
            __result = GrammarResolverSimple.Formatted(str, argsLabels, argsObjects);
            return false;
        }    

        public static bool Formatted(ref TaggedString __result, string str, params NamedArgument[] args)
        {
            List<string> argsLabels = getArgsLabels();
            List<object> argsObjects = getArgsObjects();

            argsLabels.Clear();
            argsObjects.Clear();
            for (int i = 0; i < args.Length; i++)
            {
                argsLabels.Add(args[i].label);
                argsObjects.Add(args[i].arg);
            }

            __result = GrammarResolverSimple.Formatted(str, argsLabels, argsObjects);
            return false;
        }


    }
}

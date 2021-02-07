using HarmonyLib;
using System.Linq;
using Verse;
using System.Collections.Concurrent;
using System.Reflection;
using System;
using UnityEngine;
using System.Collections.Generic;
using static HarmonyLib.AccessTools;
using System.Threading;

namespace RimThreaded
{

    public static class Rand_Patch
    {
        //private static readonly Stack<ulong> stateStack2 = new Stack<ulong>();
        //public static PropertyInfo stateCompressed = AccessTools.DeclaredProperty(typeof(Rand), "StateCompressed");
        //public static uint seed = AccessTools.StaticFieldRefAccess<uint>(typeof(Rand), "seed");
        //public static uint iterations = AccessTools.StaticFieldRefAccess<uint>(typeof(Rand), "iterations");
        //public static uint iterations2 = 0;
        //public static uint seed2 = (uint)DateTime.Now.GetHashCode();
        [ThreadStatic]
        public static List<int> tmpRange;

        public static uint seed = StaticFieldRefAccess<uint>(typeof(Rand), "seed");
        public static uint iterations = StaticFieldRefAccess<uint>(typeof(Rand), "iterations");
        public static Stack<ulong> stateStack = StaticFieldRefAccess<Stack<ulong>>(typeof(Rand), "stateStack");

        public static Dictionary<int, List<int>> tmpRanges = new Dictionary<int, List<int>>();
        public static List<int> getTmpRange()
        {
            int tID = Thread.CurrentThread.ManagedThreadId;
            if (!tmpRanges.TryGetValue(tID, out List<int> tmpRange))
            {
                tmpRange = new List<int>();
                tmpRanges[tID] = tmpRange;
            }
            return tmpRange;
        }
        public static ulong StateCompressed
        {
            get
            {
                return seed | ((ulong)iterations << 32);
            }
            set
            {
                seed = (uint)(value & uint.MaxValue);
                iterations = (uint)((value >> 32) & uint.MaxValue);
            }
        }

        public static bool TryRangeInclusiveWhere(ref bool __result,
          int from,
          int to,
          Predicate<int> predicate,
          out int value)
        {
            int num1 = to - from + 1;
            if (num1 <= 0)
            {
                value = 0;
                __result = false;
                return false;
            }
            int num2 = Mathf.Max(Mathf.RoundToInt(Mathf.Sqrt((float)num1)), 5);
            for (int index = 0; index < num2; ++index)
            {
                int num3 = Rand.RangeInclusive(from, to);
                if (predicate(num3))
                {
                    value = num3;
                    __result = true;
                    return false;
                }
            }
            //Rand.tmpRange.Clear();
            //List<int> tmpRange = getTmpRange();
            if(tmpRange == null)
            {
                tmpRange = new List<int>();
            }
            tmpRange.Clear();
            for (int index = from; index <= to; ++index)
                tmpRange.Add(index);
            tmpRange.Shuffle<int>();
            int index1 = 0;
            for (int count = tmpRange.Count; index1 < count; ++index1)
            {
                if (predicate(tmpRange[index1]))
                {
                    value = tmpRange[index1];
                    __result = true;
                    return false;
                }
            }
            value = 0;
            __result = false;
            return false;
        }

        public static bool get_Int(ref int __result)
        {
            __result = MurmurHash.GetInt(seed, iterations++);
            return false;
        }

        public static bool PushState()
        {
            lock (stateStack)
            {
                stateStack.Push(StateCompressed);
            }
            return false;
        }

        public static bool PopState()
        {
            lock (stateStack)
            {
                StateCompressed = stateStack.Pop();
            }
            return false;
        }

    }

}
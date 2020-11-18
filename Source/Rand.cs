using HarmonyLib;
using System.Linq;
using Verse;
using System.Collections.Concurrent;
using System.Reflection;
using System;
using UnityEngine;
using System.Collections.Generic;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{

    public static class Rand_Patch
    {

        public static uint seed = StaticFieldRefAccess<uint>(typeof(Rand), "seed");
        public static uint iterations = StaticFieldRefAccess<uint>(typeof(Rand), "iterations");
        public static Stack<ulong> stateStack = StaticFieldRefAccess<Stack<ulong>>(typeof(Rand), "stateStack");


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
            List<int> tmpRange = new List<int>();
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

        public static bool PushState()
        {
            ulong value = (ulong)seed | (ulong)iterations << 32;
            lock (stateStack)
            {
                stateStack.Push(value);
            }
            return false;
        }

        public static bool PopState()
        {
            ulong result2;
            lock (stateStack)
            {
                result2 = stateStack.Pop();
            }
            seed = (uint)(result2 & (ulong) uint.MaxValue);
            iterations = (uint)(result2 >> 32 & (ulong)uint.MaxValue);            

            return false;
        }

    }

}

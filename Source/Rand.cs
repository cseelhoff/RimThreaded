using HarmonyLib;
using System.Linq;
using Verse;
using System.Collections.Concurrent;
using System.Reflection;
using System;
using UnityEngine;
using System.Collections.Generic;

namespace RimThreaded
{

    public static class Rand_Patch
    {
        private static ConcurrentStack<ulong> stateStack = new ConcurrentStack<ulong>();
        //public static PropertyInfo stateCompressed = AccessTools.DeclaredProperty(typeof(Rand), "StateCompressed");
        //public static uint seed = AccessTools.StaticFieldRefAccess<uint>(typeof(Rand), "seed");
        //public static uint iterations = AccessTools.StaticFieldRefAccess<uint>(typeof(Rand), "iterations");
        //I could not get the StaticFieldRefAccess field to work properly. This is an ugly hack I'm doing by rewriting methods.
        public static uint iterations2 = 0;
        public static uint seed2 = (uint)DateTime.Now.GetHashCode();


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

        public static bool get_Int(ref int __result)
        {
             __result = MurmurHash.GetInt(seed2, iterations2++);
            return false;
        }
        public static bool get_Value(ref float __result)
        {
            __result = (float)(((double)MurmurHash.GetInt(seed2, iterations2++) - int.MinValue) / uint.MaxValue);
            return false;
        }

        public static bool set_Seed(uint value)
        {
            if (stateStack.Count == 0)
                Log.ErrorOnce("Modifying the initial rand seed. Call PushState() first. The initial rand seed should always be based on the startup time and set only once.", 825343540, false);
            seed2 = value;
            iterations2 = 0U;
            return false;
        }


        public static bool EnsureStateStackEmpty()
        {
            if (stateStack.Count <= 0)
                return false;
            Log.Warning("Random state stack is not empty. There were more calls to PushState than PopState. Fixing.", false);
            while (stateStack.Any())
                PopState();
            return false;
        }

        public static bool PushState()
        {            
            stateStack.Push(seed2 | iterations2 << 32);
            return false;
        }

        public static bool PopState()
        {
            if (stateStack.TryPop(out ulong result))
            {
                seed2 = (uint)(result & uint.MaxValue);
                iterations2 = (uint)(result >> 32 & uint.MaxValue);
            }
            else
            {
                Log.Error("Rand.PopState stateStack.TryPop failed");
            }
            return false;
        }

    }

}

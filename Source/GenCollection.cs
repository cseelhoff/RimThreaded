using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace RimThreaded
{

    public class GenCollection_Patch
    {
        internal static void RunDestructivePatches()
        {
            Type original = typeof(GenCollection);
            Type patched = typeof(GenCollection_Patch);
            MethodInfo[] genCollectionMethods = original.GetMethods();
            MethodInfo originalRemoveAll = null;
            foreach (MethodInfo mi in genCollectionMethods)
            {
                if (mi.Name.Equals("RemoveAll") && mi.GetGenericArguments().Length == 2)
                {
                    originalRemoveAll = mi;
                    break;
                }
            }

            MethodInfo originalRemoveAllGeneric = originalRemoveAll.MakeGenericMethod(new Type[] { typeof(object), typeof(object) });
            MethodInfo patchedRemoveAll = patched.GetMethod("RemoveAll_Object_Object_Patch");
            HarmonyMethod prefixRemoveAll = new HarmonyMethod(patchedRemoveAll);
            RimThreadedHarmony.harmony.Patch(originalRemoveAllGeneric, prefix: prefixRemoveAll);

        }

        public static bool RemoveAll_Object_Object_Patch(ref int __result, Dictionary<object, object> dictionary, Predicate<KeyValuePair<object, object>> predicate)
        {
            List<object> list = new List<object>();
            lock (dictionary)
            {
                foreach (KeyValuePair<object, object> item in dictionary)
                {
                    if (predicate(item))
                    {
                        list.Add(item);
                    }
                }
            }
            if (list.Count > 0)
            {
                int i = 0;
                for (int count = list.Count; i < count; i++)
                {
                    lock (dictionary)
                    {
                        dictionary.Remove(list[i]);
                    }
                }

                __result = list.Count;
                return false;
            }

            __result = 0;
            return false;

        }

    }
}

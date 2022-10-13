using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Verse;
using static RimWorld.SituationalThoughtHandler;

namespace RimThreaded.RW_Patches
{

    public class GenCollection_Patch
    {
        [ThreadStatic] static List<object> list;
        [ThreadStatic] static List<Pawn> pawnList;
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

            MethodInfo originalRemoveAllGeneric = originalRemoveAll.MakeGenericMethod(typeof(object), typeof(object));
            MethodInfo patchedRemoveAll = patched.GetMethod(nameof(RemoveAll_Object_Object_Patch));
            HarmonyMethod prefixRemoveAll = new HarmonyMethod(patchedRemoveAll);
            RimThreadedHarmony.harmony.Patch(originalRemoveAllGeneric, prefix: prefixRemoveAll);

        }

        public static bool RemoveAll_Object_Object_Patch(ref int __result, Dictionary<object, object> dictionary, Predicate<KeyValuePair<object, object>> predicate)
        {
            if (list == null)
                list = new List<object>();
            list.Clear();
            lock (dictionary)
            {
                foreach (KeyValuePair<object, object> item in dictionary)
                {
                    if (predicate(item))
                    {
                        list.Add(item.Key);
                    }
                }
                int count = list.Count;
                for (int i = 0; i < count; i++)
                {
                    dictionary.Remove(list[i]);
                }
            }
            __result = list.Count;
            return false;
        }

        public static bool RemoveAll_Pawn_CachedSocialThoughts(ref int __result,
          Dictionary<Pawn, CachedSocialThoughts> dictionary,
          Predicate<KeyValuePair<Pawn, CachedSocialThoughts>> predicate)
        {
            if (pawnList == null)
                pawnList = new List<Pawn>();
            pawnList.Clear();
            lock (dictionary)
            {
                foreach (KeyValuePair<Pawn, CachedSocialThoughts> item in dictionary)
                {
                    if (predicate(item))
                    {
                        pawnList.Add(item.Key);
                    }
                }
                int count = pawnList.Count;
                for (int i = 0; i < count; i++)
                {
                    dictionary.Remove(pawnList[i]);
                }
            }
            __result = pawnList.Count;
            return false;
        }

    }
}

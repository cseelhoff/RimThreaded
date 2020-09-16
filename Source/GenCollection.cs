using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace RimThreaded
{

    public class GenCollection_Patch
	{

        public static bool RemoveAll<TKey, TValue>(ref int __result, Dictionary<TKey, TValue> dictionary, Predicate<KeyValuePair<TKey, TValue>> predicate)
        {
            List<TKey> list = new List<TKey>(); 
            //try
            //{
                foreach (KeyValuePair<TKey, TValue> item in dictionary)
                {
                    if (predicate(item))
                    {
                        //if (list == null)
                        //{
                            //list = SimplePool<List<TKey>>.Get();
                            //list = new List<TKey>();
                        //}

                        list.Add(item.Key);
                    }
                }

                //if (list != null)
                if (list.Count > 0)
                {
                    int i = 0;
                    for (int count = list.Count; i < count; i++)
                    {
                        dictionary.Remove(list[i]);
                    }

                    __result = list.Count;
                    return false;
                }

                __result = 0;
                return false;
            //}
            //finally
            //{
                /*
                if (list != null)
                {
                    list.Clear();
                    SimplePool<List<TKey>>.Return(list);
                }
                */
            //}
        }



    }
}

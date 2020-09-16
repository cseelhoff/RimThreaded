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
        
        public static bool TryRandomElement_Pawn(IEnumerable<Pawn> source, out Pawn result)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            IList<Pawn> list = source as IList<Pawn>;
            if (list != null)
            {
                if (list.Count == 0)
                {
                    result = default(Pawn);
                    return false;
                }
            }
            else
            {
                list = source.ToList();
                if (!list.Any())
                {
                    result = default(Pawn);
                    return false;
                }
            }

            result = list.RandomElement();
            return true;
        }
        
        public static bool RemoveAll_Pawn_SituationalThoughtHandler_Patch(ref int __result, Dictionary<Pawn, SituationalThoughtHandler_Patch.CachedSocialThoughts> dictionary, Predicate<KeyValuePair<Pawn, SituationalThoughtHandler_Patch.CachedSocialThoughts>> predicate)
        {
            List<Pawn> list = new List<Pawn>();
            //try
            //{
                foreach (KeyValuePair<Pawn, SituationalThoughtHandler_Patch.CachedSocialThoughts> item in dictionary)
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

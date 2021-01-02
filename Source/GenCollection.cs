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
        public static bool TryRandomElementByWeight_Pawn(IEnumerable<Pawn> source, Func<Pawn, float> weightSelector, out Pawn result)
        {
            IList<Pawn> list = source as IList<Pawn>;
            if (list != null)
            {
                float num = 0f;
                for (int i = 0; i < list.Count; i++)
                {
                    float num2 = weightSelector(list[i]);
                    if (num2 < 0f)
                    {
                        Log.Error("Negative weight in selector: " + num2 + " from " + list[i]);
                        num2 = 0f;
                    }

                    num += num2;
                }

                if (list.Count == 1 && num > 0f)
                {
                    result = list[0];
                    return true;
                }

                if (num == 0f)
                {
                    result = default(Pawn);
                    return false;
                }

                num *= Rand.Value;
                for (int j = 0; j < list.Count; j++)
                {
                    float num3 = weightSelector(list[j]);
                    if (!(num3 <= 0f))
                    {
                        num -= num3;
                        if (num <= 0f)
                        {
                            result = list[j];
                            return true;
                        }
                    }
                }
            }

            IEnumerator<Pawn> enumerator = source.GetEnumerator();
            result = default(Pawn);
            float num4 = 0f;
            while (num4 == 0f && enumerator.MoveNext())
            {
                result = enumerator.Current;
                num4 = weightSelector(result);
                if (num4 < 0f)
                {
                    Log.Error("Negative weight in selector: " + num4 + " from " + result);
                    num4 = 0f;
                }
            }

            if (num4 == 0f)
            {
                result = default(Pawn);
                return false;
            }

            while (enumerator.MoveNext())
            {
                Pawn current = enumerator.Current;
                float num5 = weightSelector(current);
                if (num5 < 0f)
                {
                    Log.Error("Negative weight in selector: " + num5 + " from " + current);
                    num5 = 0f;
                }

                if (Rand.Range(0f, num4 + num5) >= num4)
                {
                    result = current;
                }

                num4 += num5;
            }

            return true;
        }


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

        public static bool RemoveAll_Pawn_SituationalThoughtHandler_Patch(ref int __result, Dictionary<Pawn, SituationalThoughtHandler_Patch.CachedSocialThoughts> dictionary, Predicate<KeyValuePair<Pawn, SituationalThoughtHandler_Patch.CachedSocialThoughts>> predicate)
        {
            List<Pawn> list = new List<Pawn>();
            //try
            //{
            lock (dictionary)
            {
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
            }
                //if (list != null)
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

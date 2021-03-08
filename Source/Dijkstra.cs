using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using UnityEngine;

namespace RimThreaded
{

    public class DijkstraInt
    {
        [ThreadStatic]
        private static Dictionary<int, float> distances;
        [ThreadStatic]
        private static FastPriorityQueueKeyValuePairIntFloat queue;
        [ThreadStatic]
        private static List<KeyValuePair<int, float>> tmpResult;

        private class DistanceComparer : IComparer<KeyValuePair<int, float>>
        {
            public int Compare(KeyValuePair<int, float> a, KeyValuePair<int, float> b)
            {
                return a.Value.CompareTo(b.Value);
            }
        }
        private static void HandleNeighbor(int n, float nodeDist, KeyValuePair<int, float> node, 
            Func<int, int, float> distanceGetter, Dictionary<int, int> outParents, Dictionary<int, float> distances, FastPriorityQueueKeyValuePairIntFloat queue)
        {
            float num = nodeDist + Mathf.Max(distanceGetter(node.Key, n), 0f);
            bool flag = false;
            if (distances.TryGetValue(n, out float value))
            {
                if (num < value)
                {
                    distances[n] = num;
                    flag = true;
                }
            }
            else
            {
                distances.Add(n, num);
                flag = true;
            }

            if (!flag)
            {
                return;
            }

            queue.Push(new KeyValuePair<int, float>(n, num));
            if (outParents != null)
            {
                if (outParents.ContainsKey(n))
                {
                    outParents[n] = node.Key;
                }
                else
                {
                    outParents.Add(n, node.Key);
                }
            }
        }

        public static void Run(IEnumerable<int> startingNodes, Func<int, IEnumerable<int>> neighborsGetter, 
            Func<int, int, float> distanceGetter, List<KeyValuePair<int, float>> outDistances, 
            Dictionary<int, int> outParents = null)
        {
            outDistances.Clear();
            //distances.Clear();
            if (distances == null)
            {
                distances = new Dictionary<int, float>();
            } else
            {
                distances.Clear();
            }
            //queue.Clear();
            if (queue == null)
            {
                queue = new FastPriorityQueueKeyValuePairIntFloat(new DistanceComparer());
            } else
            {
                queue.Clear();
            }
            outParents?.Clear();
            if (startingNodes is IList<int> list)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    int key = list[i];
                    if (!distances.ContainsKey(key))
                    {
                        distances.Add(key, 0f);
                        queue.Push(new KeyValuePair<int, float>(key, 0f));
                    }
                }
            }
            else
            {
                foreach (int startingNode in startingNodes)
                {
                    if (!distances.ContainsKey(startingNode))
                    {
                        distances.Add(startingNode, 0f);
                        queue.Push(new KeyValuePair<int, float>(startingNode, 0f));
                    }
                }
            }

            while (queue.Count != 0)
            {
                KeyValuePair<int, float> node = queue.Pop();
                float num = distances[node.Key];
                if (node.Value != num)
                {
                    continue;
                }

                IEnumerable<int> enumerable = neighborsGetter(node.Key);
                if (enumerable == null)
                {
                    continue;
                }

                if (enumerable is IList<int> list2)
                {
                    for (int j = 0; j < list2.Count; j++)
                    {
                        int i2;
                        try
                        {
                            i2 = list2[j];
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            break;
                        }

                        HandleNeighbor(i2, num, node, distanceGetter, outParents, distances, queue);
                    }
                }
                else
                {
                    foreach (int item in enumerable)
                    {
                        HandleNeighbor(item, num, node, distanceGetter, outParents, distances, queue);
                    }
                }
            }

            foreach (KeyValuePair<int, float> distance in distances)
            {
                outDistances.Add(distance);
            }

            //distances.Clear();
        }

        public static void Run(IEnumerable<int> startingNodes, Func<int, IEnumerable<int>> neighborsGetter, Func<int, int, float> distanceGetter, Dictionary<int, float> outDistances, Dictionary<int, int> outParents = null)
        {
            if (tmpResult == null)
            {
                tmpResult = new List<KeyValuePair<int, float>>();
            } else
            {
                tmpResult.Clear();
            }
            Run(startingNodes, neighborsGetter, distanceGetter, tmpResult, outParents);
            outDistances.Clear();
            for (int i = 0; i < tmpResult.Count; i++)
            {
                outDistances.Add(tmpResult[i].Key, tmpResult[i].Value);
            }

            //tmpResult.Clear();
        }


    }
}

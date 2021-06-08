using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RimThreaded
{
    public static class Dijkstra_Patch<T>
    {
        [ThreadStatic] public static Dictionary<T, float> distances;
        [ThreadStatic] public static FastPriorityQueue<KeyValuePair<T, float>> queue;
        [ThreadStatic] public static List<T> singleNodeList;
        [ThreadStatic] public static List<KeyValuePair<T, float>> tmpResult;

        public static void InitializeThreadStatics()
        {
            distances = new Dictionary<T, float>();
            queue = new FastPriorityQueue<KeyValuePair<T, float>>(new DistanceComparer());
            singleNodeList = new List<T>();
            tmpResult = new List<KeyValuePair<T, float>>();
        }

        public static void InitializeQueue()
        {
            queue = new FastPriorityQueue<KeyValuePair<T, float>>(new DistanceComparer());
        }

        private class DistanceComparer : IComparer<KeyValuePair<T, float>>
        {
            public int Compare(KeyValuePair<T, float> a, KeyValuePair<T, float> b)
            {
                return a.Value.CompareTo(b.Value);
            }
        }

        public static void Run(T startingNode, Func<T, IEnumerable<T>> neighborsGetter,
            Func<T, T, float> distanceGetter, List<KeyValuePair<T, float>> outDistances,
            Dictionary<T, T> outParents = null)
        {
            singleNodeList.Clear();
            singleNodeList.Add(startingNode);
            Run(singleNodeList, neighborsGetter, distanceGetter, outDistances, outParents);
        }

        public static void Run(IEnumerable<T> startingNodes, Func<T, IEnumerable<T>> neighborsGetter, Func<T, T, float> distanceGetter, List<KeyValuePair<T, float>> outDistances, Dictionary<T, T> outParents = null)
        {
            outDistances.Clear();
            distances.Clear();
            queue.Clear();
            outParents?.Clear();
            IList<T> list = startingNodes as IList<T>;
            if (list != null)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    T key = list[i];
                    if (!distances.ContainsKey(key))
                    {
                        distances.Add(key, 0f);
                        queue.Push(new KeyValuePair<T, float>(key, 0f));
                    }
                }
            }
            else
            {
                foreach (T startingNode in startingNodes)
                {
                    if (!distances.ContainsKey(startingNode))
                    {
                        distances.Add(startingNode, 0f);
                        queue.Push(new KeyValuePair<T, float>(startingNode, 0f));
                    }
                }
            }

            while (queue.Count != 0)
            {
                KeyValuePair<T, float> node = queue.Pop();
                float num = distances[node.Key];
                if (node.Value != num)
                {
                    continue;
                }

                IEnumerable<T> enumerable = neighborsGetter(node.Key);
                if (enumerable == null)
                {
                    continue;
                }

                IList<T> list2 = enumerable as IList<T>;
                if (list2 != null)
                {
                    for (int j = 0; j < list2.Count; j++)
                    {
                        HandleNeighbor(list2[j], num, node, distanceGetter, outParents);
                    }

                    continue;
                }

                foreach (T item in enumerable)
                {
                    HandleNeighbor(item, num, node, distanceGetter, outParents);
                }
            }

            foreach (KeyValuePair<T, float> distance in distances)
            {
                outDistances.Add(distance);
            }

            distances.Clear();
        }

        public static void Run(T startingNode, Func<T, IEnumerable<T>> neighborsGetter, Func<T, T, float> distanceGetter, Dictionary<T, float> outDistances, Dictionary<T, T> outParents = null)
        {
            singleNodeList.Clear();
            singleNodeList.Add(startingNode);
            Run(singleNodeList, neighborsGetter, distanceGetter, outDistances, outParents);
        }

        public static void Run(IEnumerable<T> startingNodes, Func<T, IEnumerable<T>> neighborsGetter, Func<T, T, float> distanceGetter, Dictionary<T, float> outDistances, Dictionary<T, T> outParents = null)
        {
            Run(startingNodes, neighborsGetter, distanceGetter, tmpResult, outParents);
            outDistances.Clear();
            for (int i = 0; i < tmpResult.Count; i++)
            {
                outDistances.Add(tmpResult[i].Key, tmpResult[i].Value);
            }

            tmpResult.Clear();
        }

        private static void HandleNeighbor(T n, float nodeDist, KeyValuePair<T, float> node, Func<T, T, float> distanceGetter, Dictionary<T, T> outParents)
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

            queue.Push(new KeyValuePair<T, float>(n, num));
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
    }
}

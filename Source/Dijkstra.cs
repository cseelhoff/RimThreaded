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

    public class DijkstraIntVec3
    {
        private Dictionary<IntVec3, float> distances = new Dictionary<IntVec3, float>();
        private FastPriorityQueue<KeyValuePair<IntVec3, float>> queue = new FastPriorityQueue<KeyValuePair<IntVec3, float>>((IComparer<KeyValuePair<IntVec3, float>>)new DistanceComparer());
        //private FastPriorityQueueKeyValuePairIntVec3Float_Patch queue = new FastPriorityQueue<KeyValuePair<IntVec3, float>>((IComparer<KeyValuePair<IntVec3, float>>)new DistanceComparer());
        private static List<IntVec3> singleNodeList = new List<IntVec3>();
        private List<KeyValuePair<IntVec3, float>> tmpResult = new List<KeyValuePair<IntVec3, float>>();

        public static void Run(
          IntVec3 startingNode,
          Func<IntVec3, IEnumerable<IntVec3>> neighborsGetter,
          Func<IntVec3, IntVec3, float> distanceGetter,
          List<KeyValuePair<IntVec3, float>> outDistances,
          Dictionary<IntVec3, IntVec3> outParents = null)
        {
            singleNodeList.Clear();
            singleNodeList.Add(startingNode);
            Run((IEnumerable<IntVec3>)singleNodeList, neighborsGetter, distanceGetter, outDistances, outParents);
        }

        public static void Run(
          IEnumerable<IntVec3> startingNodes,
          Func<IntVec3, IEnumerable<IntVec3>> neighborsGetter,
          Func<IntVec3, IntVec3, float> distanceGetter,
          List<KeyValuePair<IntVec3, float>> outDistances,
          Dictionary<IntVec3, IntVec3> outParents = null)
        {
            outDistances.Clear();
            //distances.Clear();
            Dictionary<IntVec3, float> distances = new Dictionary<IntVec3, float>();
            //queue.Clear();
            FastPriorityQueue<KeyValuePair<IntVec3, float>> queue = new FastPriorityQueue<KeyValuePair<IntVec3, float>>((IComparer<KeyValuePair<IntVec3, float>>)new DistanceComparer());
            outParents?.Clear();
            if (startingNodes is IList<IntVec3> objList)
            {
                for (int index = 0; index < objList.Count; ++index)
                {
                    IntVec3 key;
                    try
                    {
                        key = objList[index];
                    } catch (ArgumentOutOfRangeException)
                    {
                        break;
                    }
                    if (!distances.ContainsKey(key))
                    {
                        distances.Add(key, 0.0f);
                        queue.Push(new KeyValuePair<IntVec3, float>(key, 0.0f));
                    }
                }
            }
            else
            {
                foreach (IntVec3 startingNode in startingNodes)
                {
                    if (!distances.ContainsKey(startingNode))
                    {
                        distances.Add(startingNode, 0.0f);
                        queue.Push(new KeyValuePair<IntVec3, float>(startingNode, 0.0f));
                    }
                }
            }
            while (queue.Count > 0)
            {
                KeyValuePair<IntVec3, float> node = queue.Pop();
                {
                    IntVec3 nodeKey = node.Key;
                    // NEED TO FIX HACK (contains key)
                    if (distances.ContainsKey(nodeKey))
                    {
                        float distance = distances[nodeKey];
                        if ((double)node.Value == (double)distance)
                        {
                            IEnumerable<IntVec3> objs = neighborsGetter(node.Key);
                            if (objs != null)
                            {
                                if (objs is IList<IntVec3> objList2)
                                {
                                    for (int index = 0; index < objList2.Count; ++index)
                                        HandleNeighbor2(distances, queue, objList2[index], distance, node, distanceGetter, outParents);
                                }
                                else
                                {
                                    foreach (IntVec3 n in objs)
                                        HandleNeighbor2(distances, queue, n, distance, node, distanceGetter, outParents);
                                }
                            }
                        }
                    }
                }
            }
            foreach (KeyValuePair<IntVec3, float> distance in distances)
                outDistances.Add(distance);
            distances.Clear();
        }

        public void Run(
          IntVec3 startingNode,
          Func<IntVec3, IEnumerable<IntVec3>> neighborsGetter,
          Func<IntVec3, IntVec3, float> distanceGetter,
          Dictionary<IntVec3, float> outDistances,
          Dictionary<IntVec3, IntVec3> outParents = null)
        {
            singleNodeList.Clear();
            singleNodeList.Add(startingNode);
            Run((IEnumerable<IntVec3>)singleNodeList, neighborsGetter, distanceGetter, outDistances, outParents);
        }

        public void Run(
          IEnumerable<IntVec3> startingNodes,
          Func<IntVec3, IEnumerable<IntVec3>> neighborsGetter,
          Func<IntVec3, IntVec3, float> distanceGetter,
          Dictionary<IntVec3, float> outDistances,
          Dictionary<IntVec3, IntVec3> outParents = null)
        {
            Run(startingNodes, neighborsGetter, distanceGetter, tmpResult, outParents);
            outDistances.Clear();
            for (int index = 0; index < tmpResult.Count; ++index)
            {
                Dictionary<IntVec3, float> dictionary = outDistances;
                KeyValuePair<IntVec3, float> keyValuePair = tmpResult[index];
                IntVec3 key = keyValuePair.Key;
                keyValuePair = tmpResult[index];
                double num = (double)keyValuePair.Value;
                dictionary.Add(key, (float)num);
            }
            tmpResult.Clear();
        }

        private static void HandleNeighbor2(Dictionary<IntVec3, float> distances, 
            FastPriorityQueue<KeyValuePair<IntVec3, float>> queue,
          IntVec3 n,
          float nodeDist,
          KeyValuePair<IntVec3, float> node,
          Func<IntVec3, IntVec3, float> distanceGetter,
          Dictionary<IntVec3, IntVec3> outParents)
        {
            float num1 = nodeDist + Mathf.Max(distanceGetter(node.Key, n), 0.0f);
            bool flag = false;
            float num2;
            if (distances.TryGetValue(n, out num2))
            {
                if ((double)num1 < (double)num2)
                {
                    distances[n] = num1;
                    flag = true;
                }
            }
            else
            {
                distances.Add(n, num1);
                flag = true;
            }
            if (!flag)
                return;
            queue.Push(new KeyValuePair<IntVec3, float>(n, num1));
            if (outParents == null)
                return;
            if (outParents.ContainsKey(n))
                outParents[n] = node.Key;
            else
                outParents.Add(n, node.Key);
        }

        public class DistanceComparer : IComparer<KeyValuePair<IntVec3, float>>
        {
            public int Compare(KeyValuePair<IntVec3, float> a, KeyValuePair<IntVec3, float> b)
            {
                return a.Value.CompareTo(b.Value);
            }
        }
    }

    public class DijkstraInt
    {
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
            Dictionary<int, float> distances = new Dictionary<int, float>();
            //queue.Clear();
            FastPriorityQueueKeyValuePairIntFloat queue = new FastPriorityQueueKeyValuePairIntFloat(new DistanceComparer());
            outParents?.Clear();
            IList<int> list = startingNodes as IList<int>;
            if (list != null)
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

                IList<int> list2 = enumerable as IList<int>;
                if (list2 != null)
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

            distances.Clear();
        }

        public static void Run(IEnumerable<int> startingNodes, Func<int, IEnumerable<int>> neighborsGetter, Func<int, int, float> distanceGetter, Dictionary<int, float> outDistances, Dictionary<int, int> outParents = null)
        {
            List<KeyValuePair<int, float>> tmpResult = new List<KeyValuePair<int, float>>();
            Run(startingNodes, neighborsGetter, distanceGetter, tmpResult, outParents);
            outDistances.Clear();
            for (int i = 0; i < tmpResult.Count; i++)
            {
                outDistances.Add(tmpResult[i].Key, tmpResult[i].Value);
            }

            tmpResult.Clear();
        }


    }
}

using HarmonyLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Verse;
using Verse.AI;

namespace RimThreaded
{

    public class RegionTraverser_Patch
    {
        public static Dictionary<int, RegionTraverser2> regionTraverser2Dict = new Dictionary<int, RegionTraverser2>();
        public static bool BreadthFirstTraverse(
            Region root,
            RegionEntryPredicate entryCondition,
            RegionProcessor regionProcessor,
            int maxRegions = 999999,
            RegionType traversableRegionTypes = RegionType.Set_Passable)
        {
            RegionTraverser2 regionTraverser;
            int t = Thread.CurrentThread.ManagedThreadId;
            lock (regionTraverser2Dict)
            {
                if (!regionTraverser2Dict.TryGetValue(t, out regionTraverser))
                {
                    regionTraverser = new RegionTraverser2();
                    regionTraverser2Dict.Add(t, regionTraverser);
                }
            }
                
            if (regionTraverser.freeWorkers.Count == 0)
                Log.Error("No free workers for breadth-first traversal. Either BFS recurred deeper than " + regionTraverser.NumWorkers + ", or a bug has put this system in an inconsistent state. Resetting.", false);
            else if (root == null)
            {
                Log.Error("BreadthFirstTraverse with null root region.", false);
            }
            else
            {
                BFSWorker_Patch bfsWorker = regionTraverser.freeWorkers.Dequeue();
                try
                {
                    bfsWorker.BreadthFirstTraverseWork(root, entryCondition, regionProcessor, maxRegions, traversableRegionTypes);
                }
                catch (Exception ex)
                {
                    Log.Error("Exception in BreadthFirstTraverse: " + ex.ToString(), false);
                }
                finally
                {
                    bfsWorker.Clear();
                    regionTraverser.freeWorkers.Enqueue(bfsWorker);
                }
            }
            return false;
        }
    }
    
    

}

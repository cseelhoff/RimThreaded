using System;
using System.Collections.Generic;
using Verse;

namespace RimThreaded
{
    class RegionTraverser_Patch
    {
        [ThreadStatic] public static Queue<RegionTraverser.BFSWorker> freeWorkers;
        [ThreadStatic] public static int NumWorkers;

		public static void InitializeThreadStatics()
		{
            NumWorkers = 8;
			freeWorkers = new Queue<RegionTraverser.BFSWorker>(NumWorkers);
            for (int closedArrayPos = 0; closedArrayPos < NumWorkers; closedArrayPos++)
            {
                freeWorkers.Enqueue(new RegionTraverser.BFSWorker(closedArrayPos));
            }
        }

        //public static bool RecreateWorkers()
        //{
        //    freeWorkers.Clear();
        //    Log.Message(System.Threading.Thread.CurrentThread.ManagedThreadId.ToString());
        //    for (int closedArrayPos = 0; closedArrayPos < NumWorkers; closedArrayPos++)
        //    {
        //        freeWorkers.Enqueue(new RegionTraverser.BFSWorker(closedArrayPos));
        //    }
        //    return false;
        //}
        //public static bool BreadthFirstTraverse(Region root, RegionEntryPredicate entryCondition, RegionProcessor regionProcessor, int maxRegions = 999999, RegionType traversableRegionTypes = RegionType.Set_Passable)
        //{
        //    Queue<RegionTraverser.BFSWorker> f = freeWorkers;

        //    if (f.Count == 0)
        //    {
        //        Log.Message(System.Threading.Thread.CurrentThread.ManagedThreadId.ToString());
        //        Log.Error("No free workers for breadth-first traversal. Either BFS recurred deeper than " + NumWorkers + ", or a bug has put this system in an inconsistent state. Resetting.");
        //        return false;
        //    }
        //    if (root == null)
        //    {
        //        Log.Error("BreadthFirstTraverse with null root region.");
        //        return false;
        //    }
        //    RegionTraverser.BFSWorker bfsWorker = f.Dequeue();
        //    try
        //    {
        //        bfsWorker.BreadthFirstTraverseWork(root, entryCondition, regionProcessor, maxRegions, traversableRegionTypes);
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error("Exception in BreadthFirstTraverse: " + ex.ToString());
        //    }
        //    finally
        //    {
        //        bfsWorker.Clear();
        //        f.Enqueue(bfsWorker);
        //    }


        //    return false;
        //}

    }
}

using System;
using System.Collections.Generic;
using Verse;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{
    class RegionTraverser_Transpile
    {
        [ThreadStatic] public static Dictionary<Region, uint[]> regionClosedIndex;
        [ThreadStatic] public static Queue<RegionTraverser.BFSWorker> freeWorkers;
        [ThreadStatic] public static int NumWorkers;

		public static void InitializeThreadStatics()
		{
			regionClosedIndex = new Dictionary<Region, uint[]>();
            NumWorkers = 8;
			freeWorkers = new Queue<RegionTraverser.BFSWorker>(NumWorkers);
            for (int closedArrayPos = 0; closedArrayPos < NumWorkers; closedArrayPos++)
            {
                freeWorkers.Enqueue(new RegionTraverser.BFSWorker(closedArrayPos));
            }
        }

        public static Queue<RegionTraverser.BFSWorker> get_freeWorkers()
        {
            return freeWorkers;
        }


        public static void RunNonDestructivePatches()
		{
			Type original = typeof(RegionTraverser);
            Type patched = typeof(RegionTraverser_Transpile);
			RimThreadedHarmony.AddAllMatchingFields(original, patched);
            RimThreadedHarmony.replaceFields.Add(Field(typeof(Region), "closedIndex"), Method(typeof(RegionTraverser_Transpile), "GetRegionClosedIndex"));

            RimThreadedHarmony.TranspileFieldReplacements(original, "BreadthFirstTraverse", new[] {
                typeof(Region),
                typeof(RegionEntryPredicate),
                typeof(RegionProcessor),
                typeof(int),
                typeof(RegionType)
            });
            RimThreadedHarmony.TranspileFieldReplacements(original, "RecreateWorkers");
            RimThreadedHarmony.harmony.Patch(Constructor(original), transpiler: RimThreadedHarmony.replaceFieldsHarmonyTranspiler);
            //RimThreadedHarmony.Prefix(original, patched, "BreadthFirstTraverse", new[] {
            //    typeof(Region),
            //    typeof(RegionEntryPredicate),
            //    typeof(RegionProcessor),
            //    typeof(int),
            //    typeof(RegionType)
            //});
            //RimThreadedHarmony.Prefix(original, patched, "RecreateWorkers");

            original = typeof(RegionTraverser.BFSWorker);
            RimThreadedHarmony.TranspileFieldReplacements(original, "QueueNewOpenRegion");
            RimThreadedHarmony.TranspileFieldReplacements(original, "BreadthFirstTraverseWork");
            
        }
		public static uint[] GetRegionClosedIndex(Region region)
		{
            if (regionClosedIndex.TryGetValue(region, out uint[] closedIndex)) return closedIndex;
            closedIndex = new uint[8];
            regionClosedIndex[region] = closedIndex;
            return closedIndex;
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

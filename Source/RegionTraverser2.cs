using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;

namespace RimThreaded
{
    
    public class RegionTraverser2
    {
        public Queue<BFSWorker_Patch2> freeWorkers = new Queue<BFSWorker_Patch2>();
        public int NumWorkers = 8;
        public readonly RegionEntryPredicate PassAll = ((from, to) => true);
        public Dictionary<Region, uint[]> regionClosedIndex = new Dictionary<Region, uint[]>();

        public RegionTraverser2()
        {
            RecreateWorkers();
        }

        public Room FloodAndSetRooms(Region root, Map map, Room existingRoom)
        {
            Room floodingRoom = existingRoom != null ? existingRoom : Room.MakeNew(map);
            root.Room = floodingRoom;
            if (!root.type.AllowsMultipleRegionsPerRoom())
                return floodingRoom;
            RegionEntryPredicate entryCondition = ((from, r) => r.type == root.type && r.Room != floodingRoom);
            RegionProcessor regionProcessor = (r =>
            {
                r.Room = floodingRoom;
                return false;
            });
            BreadthFirstTraverse(root, entryCondition, regionProcessor, 999999, RegionType.Set_All);
            return floodingRoom;
        }

        public void FloodAndSetNewRegionIndex(Region root, int newRegionGroupIndex)
        {
            root.newRegionGroupIndex = newRegionGroupIndex;
            if (!root.type.AllowsMultipleRegionsPerRoom())
                return;
            RegionEntryPredicate entryCondition = ((from, r) => r.type == root.type && r.newRegionGroupIndex < 0);
            RegionProcessor regionProcessor = (r =>
            {
                r.newRegionGroupIndex = newRegionGroupIndex;
                return false;
            });
            BreadthFirstTraverse(root, entryCondition, regionProcessor, 999999, RegionType.Set_All);
        }

        public bool WithinRegions(
          IntVec3 A,
          IntVec3 B,
          Map map,
          int regionLookCount,
          TraverseParms traverseParams,
          RegionType traversableRegionTypes = RegionType.Set_Passable)
        {
            Region region = A.GetRegion(map, traversableRegionTypes);
            if (region == null)
                return false;
            Region regB = B.GetRegion(map, traversableRegionTypes);
            if (regB == null)
                return false;
            if (region == regB)
                return true;
            RegionEntryPredicate entryCondition = ((from, r) => r.Allows(traverseParams, false));
            bool found = false;
            RegionProcessor regionProcessor = (r =>
            {
                if (r != regB)
                    return false;
                found = true;
                return true;
            });
            BreadthFirstTraverse(region, entryCondition, regionProcessor, regionLookCount, traversableRegionTypes);
            return found;
        }

        public void MarkRegionsBFS(
          Region root,
          RegionEntryPredicate entryCondition,
          int maxRegions,
          int inRadiusMark,
          RegionType traversableRegionTypes = RegionType.Set_Passable)
        {
            BreadthFirstTraverse(root, entryCondition, r =>
            {
                r.mark = inRadiusMark;
                return false;
            }, maxRegions, traversableRegionTypes);
        }

        public void RecreateWorkers()
        {
            freeWorkers.Clear();
            for (int closedArrayPos = 0; closedArrayPos < NumWorkers; ++closedArrayPos)
                freeWorkers.Enqueue(new BFSWorker_Patch2(closedArrayPos, this));
        }

        public void BreadthFirstTraverse(
          IntVec3 start,
          Map map,
          RegionEntryPredicate entryCondition,
          RegionProcessor regionProcessor,
          int maxRegions = 999999,
          RegionType traversableRegionTypes = RegionType.Set_Passable)
        {
            Region region = start.GetRegion(map, traversableRegionTypes);
            if (region == null)
                return;
            BreadthFirstTraverse(region, entryCondition, regionProcessor, maxRegions, traversableRegionTypes);
        }

        public void BreadthFirstTraverse(
          Region root,
          RegionEntryPredicate entryCondition,
          RegionProcessor regionProcessor,
          int maxRegions = 999999,
          RegionType traversableRegionTypes = RegionType.Set_Passable)
        {
            if (freeWorkers.Count == 0)
                Log.Error("No free workers for breadth-first traversal. Either BFS recurred deeper than " + NumWorkers + ", or a bug has put this system in an inconsistent state. Resetting.", false);
            else if (root == null)
            {
                Log.Error("BreadthFirstTraverse with null root region.", false);
            }
            else
            {
                BFSWorker_Patch2 bfsWorker = freeWorkers.Dequeue();
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
                    freeWorkers.Enqueue(bfsWorker);
                }
            }
        }
    }

}

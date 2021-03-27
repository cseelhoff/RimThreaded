using System;
using System.Collections.Generic;
using Verse;

namespace RimThreaded
{
    public class BFSWorker_Patch
    {
        [ThreadStatic] public static Dictionary<Region, uint[]> regionClosedIndex;

        public static void InitializeThreadStatics()
        {
            regionClosedIndex = new Dictionary<Region, uint[]>();
        }

        public static uint[] getRegionClosedIndex(Region region)
        {
            if (!regionClosedIndex.TryGetValue(region, out uint[] closedIndex))
            {
                closedIndex = new uint[8];
                regionClosedIndex[region] = closedIndex;
            }
            return closedIndex;
        }
        private Queue<Region> open = new Queue<Region>();
        private uint closedIndex = 1;
        private int numRegionsProcessed;
        private int closedArrayPos;
        private const int skippableRegionSize = 4;
        private RegionTraverser2 regionTraverser;

        public BFSWorker_Patch(int closedArrayPos, RegionTraverser2 regionTraverser)
        {
            this.closedArrayPos = closedArrayPos;
            this.regionTraverser = regionTraverser;
        }

        public void Clear()
        {
            open.Clear();
        }

        private void QueueNewOpenRegion(Region region)
        {
            uint[] regionClosedIndex = getRegionClosedIndex(region);
            if (regionClosedIndex[closedArrayPos] == closedIndex)
                throw new InvalidOperationException("Region is already closed; you can't open it. Region: " + region.ToString());
            open.Enqueue(region);
            regionClosedIndex[closedArrayPos] = closedIndex;
        }

        private void FinalizeSearch()
        {
        }

        public void BreadthFirstTraverseWork(
            Region root,
            RegionEntryPredicate entryCondition,
            RegionProcessor regionProcessor,
            int maxRegions,
            RegionType traversableRegionTypes)
        {
            if ((root.type & traversableRegionTypes) == RegionType.None)
                return;
            ++closedIndex;
            open.Clear();
            numRegionsProcessed = 0;
            QueueNewOpenRegion(root);
            while (open.Count > 0)
            {
                Region region1 = open.Dequeue();
                if (DebugViewSettings.drawRegionTraversal)
                    region1.Debug_Notify_Traversed();
                if (regionProcessor != null)
                {
                    bool rpflag = false;
                    try { rpflag = regionProcessor(region1); }
                    catch (NullReferenceException) { }
                    if (rpflag)
                    {
                        FinalizeSearch();
                        return;
                    }
                }
                if (!region1.IsDoorway)
                    ++numRegionsProcessed;
                if (numRegionsProcessed >= maxRegions)
                {
                    FinalizeSearch();
                    return;
                }
                for (int index1 = 0; index1 < region1.links.Count; ++index1)
                {
                    RegionLink link = region1.links[index1];
                    for (int index2 = 0; index2 < 2; ++index2)
                    {
                        Region region2 = link.regions[index2];
                        if (null != region2 && regionTraverser.regionClosedIndex.ContainsKey(region2) == false)
                        {
                            regionTraverser.regionClosedIndex.Add(region2, new uint[8]);
                        }
                        if (region2 != null && (int)regionTraverser.regionClosedIndex[region2][closedArrayPos] != (int)closedIndex && (region2.type & traversableRegionTypes) != RegionType.None && (entryCondition == null || entryCondition(region1, region2)))
                            QueueNewOpenRegion(region2);
                    }
                }
            }
            FinalizeSearch();
        }
    }
}
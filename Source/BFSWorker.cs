using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;

namespace RimThreaded
{
    public class BFSWorker_Patch
    {
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
            this.open.Clear();
        }

        private void QueueNewOpenRegion(Region region)
        {
            if (regionTraverser.regionClosedIndex.ContainsKey(region) == false)
            {
                regionTraverser.regionClosedIndex.Add(region, new uint[8]);
            }

            if ((int)regionTraverser.regionClosedIndex[region][this.closedArrayPos] == (int)this.closedIndex)
                throw new InvalidOperationException("Region is already closed; you can't open it. Region: " + region.ToString());
            this.open.Enqueue(region);
            if (regionTraverser.regionClosedIndex.ContainsKey(region) == false)
            {
                regionTraverser.regionClosedIndex.Add(region, new uint[8]);
            }
            regionTraverser.regionClosedIndex[region][this.closedArrayPos] = this.closedIndex;
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
            ++this.closedIndex;
            this.open.Clear();
            this.numRegionsProcessed = 0;
            this.QueueNewOpenRegion(root);
            while (this.open.Count > 0)
            {
                Region region1 = this.open.Dequeue();
                if (DebugViewSettings.drawRegionTraversal)
                    region1.Debug_Notify_Traversed();
                if (regionProcessor != null)
                {
                    bool rpflag = false;
                    try { rpflag = regionProcessor(region1); }
                    catch (NullReferenceException) { }
                    if (rpflag)
                    {
                        this.FinalizeSearch();
                        return;
                    }
                }
                if (!region1.IsDoorway)
                    ++this.numRegionsProcessed;
                if (this.numRegionsProcessed >= maxRegions)
                {
                    this.FinalizeSearch();
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
                        if (region2 != null && (int)regionTraverser.regionClosedIndex[region2][this.closedArrayPos] != (int)this.closedIndex && (region2.type & traversableRegionTypes) != RegionType.None && (entryCondition == null || entryCondition(region1, region2)))
                            this.QueueNewOpenRegion(region2);
                    }
                }
            }
            this.FinalizeSearch();
        }
    }
}

using System;
using System.Collections.Generic;
using Verse;

namespace RimThreaded
{
    class RegionTraverser_Patch
    {
		/*
		public static Dictionary<Region, uint[]> regionClosedIndex = new Dictionary<Region, uint[]>();
		private static Queue<BFSWorker> freeWorkers = new Queue<BFSWorker>();
		public static int NumWorkers = 8;
		*/
        internal static void RunDestructivePatches()
		{
			Type original = typeof(RegionTraverser);
			Type patched = typeof(RegionTraverser_Patch);
			//RimThreadedHarmony.Prefix(original, patched, "RecreateWorkers");
            RimThreadedHarmony.Prefix(original, patched, "BreadthFirstTraverse", new Type[] { typeof(IntVec3), typeof(Map), typeof(RegionEntryPredicate), typeof(RegionProcessor), typeof(int), typeof(RegionType) });
			//RimThreadedHarmony.Prefix(original, patched, "BreadthFirstTraverse", new Type[] { typeof(Region), typeof(RegionEntryPredicate), typeof(RegionProcessor), typeof(int), typeof(RegionType) });
            //RecreateWorkers();

        }
		/*
		public static bool RecreateWorkers()
		{
			freeWorkers?.Clear();
			for (int i = 0; i < NumWorkers; i++)
			{
				freeWorkers?.Enqueue(new BFSWorker(i));
			}
            return false;
		}
		*/
		public static bool BreadthFirstTraverse(IntVec3 start, Map map, RegionEntryPredicate entryCondition, RegionProcessor regionProcessor, int maxRegions = 999999, RegionType traversableRegionTypes = RegionType.Set_Passable)
		{
			Region region = start.GetRegion(map, traversableRegionTypes);
			if (region?.type != null)
			{
                RegionTraverser.BreadthFirstTraverse(region, entryCondition, regionProcessor, maxRegions, traversableRegionTypes);
            }
			return false;
        }
		/*
		// Token: 0x06000CD6 RID: 3286 RVA: 0x000494F0 File Offset: 0x000476F0
		public static bool BreadthFirstTraverse(Region root, RegionEntryPredicate entryCondition, RegionProcessor regionProcessor, int maxRegions = 999999, RegionType traversableRegionTypes = RegionType.Set_Passable)
		{
			if (freeWorkers?.Count == 0)
			{
				Log.Error("No free workers for breadth-first traversal. Either BFS recurred deeper than " + NumWorkers + ", or a bug has put this system in an inconsistent state. Resetting.");
				return false;
			}
			if (root == null)
			{
				Log.Error("BreadthFirstTraverse with null root region.");
				return false;
			}
            BFSWorker bfsworker = freeWorkers?.Dequeue();
			try
			{
				bfsworker?.BreadthFirstTraverseWork(root, entryCondition, regionProcessor, maxRegions, traversableRegionTypes);
			}
			catch (Exception ex)
			{
				Log.Error("Exception in BreadthFirstTraverse: " + ex);
			}
			finally
			{
				bfsworker?.Clear();
				freeWorkers?.Enqueue(bfsworker);
			}

			return false;
		}


		// Token: 0x04000A23 RID: 2595
		public static readonly RegionEntryPredicate PassAll = (@from, to) => true;

		// Token: 0x0200149E RID: 5278
		private class BFSWorker
		{
            public static uint[] getRegionClosedIndex(Region region)
            {
                bool flag = regionClosedIndex == null;
                if (flag)
                {
                    regionClosedIndex = new Dictionary<Region, uint[]>();
                }
                uint[] closedIndex;
                bool flag2 = !regionClosedIndex.TryGetValue(region, out closedIndex);
                if (flag2)
                {
                    closedIndex = new uint[8];
                    regionClosedIndex[region] = closedIndex;
                }
                return closedIndex;
            }

			// Token: 0x06007DB8 RID: 32184 RVA: 0x002B6790 File Offset: 0x002B4990
			public BFSWorker(int closedArrayPos)
            {
                this.closedArrayPos = closedArrayPos;
            }

			// Token: 0x06007DB9 RID: 32185 RVA: 0x002B67B1 File Offset: 0x002B49B1
			public void Clear()
			{
				open?.Clear();
			}

			// Token: 0x06007DBA RID: 32186 RVA: 0x002B67C0 File Offset: 0x002B49C0
            private void QueueNewOpenRegion(Region region)
            {
                uint[] regionClosedIndex = getRegionClosedIndex(region);
                bool flag = regionClosedIndex[closedArrayPos] == closedIndex;
                if (flag)
                {
					Log.Error("Index: " + closedIndex + " Total:" + regionClosedIndex.Length + " Workers: " + NumWorkers);
                    //throw new InvalidOperationException("Region is already closed; you can't open it. Region: " + region?.ToString());
                }
                open?.Enqueue(region);
                regionClosedIndex[closedArrayPos] = closedIndex;
            }

			// Token: 0x06007DBB RID: 32187 RVA: 0x00002681 File Offset: 0x00000881
			private void FinalizeSearch()
			{
			}

			// Token: 0x06007DBC RID: 32188 RVA: 0x002B6818 File Offset: 0x002B4A18
			public void BreadthFirstTraverseWork(Region root, RegionEntryPredicate entryCondition, RegionProcessor regionProcessor, int maxRegions, RegionType traversableRegionTypes)
			{
				bool flag = (root?.type & traversableRegionTypes) == RegionType.None;
				if (!flag)
				{
					closedIndex += 1U;
					open?.Clear();
					numRegionsProcessed = 0;
					QueueNewOpenRegion(root);
					while (open.Count > 0)
					{
						Region region = open?.Dequeue();
						bool drawRegionTraversal = DebugViewSettings.drawRegionTraversal;
						if (drawRegionTraversal)
						{
							region?.Debug_Notify_Traversed();
						}
						bool flag2 = regionProcessor != null;
						if (flag2)
						{
							bool rpflag = false;
							try
							{
								rpflag = regionProcessor(region);
							}
							catch (NullReferenceException)
							{
							}
							bool flag3 = rpflag;
							if (flag3)
							{
								FinalizeSearch();
								return;
							}
						}
						bool flag4 = !region?.IsDoorway != null;
						if (flag4)
						{
							numRegionsProcessed++;
						}
						bool flag5 = numRegionsProcessed >= maxRegions;
						if (flag5)
						{
							FinalizeSearch();
							return;
						}
						if (region?.links?.Count != null)
                        {
							for (int index = 0; index < region.links.Count; index++)
                            {
                                RegionLink link = region.links[index];
                                for (int index2 = 0; index2 < 2; index2++)
                                {
                                    Region region2 = link?.regions[index2];
                                    bool flag6 = region2 != null && !regionClosedIndex.ContainsKey(region2);
                                    if (flag6)
                                    {
                                        regionClosedIndex?.Add(region2, new uint[8]);
                                    }
                                    bool flag7 = region2 != null && regionClosedIndex[region2][closedArrayPos] != closedIndex && (region2.type & traversableRegionTypes) != RegionType.None && (entryCondition == null || entryCondition(region, region2));
                                    if (flag7)
                                    {
                                        QueueNewOpenRegion(region2);
                                    }
                                }
                            }
                        }
					}
					FinalizeSearch();
				}
			}

            // Token: 0x04004F26 RID: 20262
			private Queue<Region> open = new Queue<Region>();

			// Token: 0x04004F27 RID: 20263
			private int numRegionsProcessed;

			// Token: 0x04004F28 RID: 20264
			private uint closedIndex = 1U;

			// Token: 0x04004F29 RID: 20265
			private int closedArrayPos;

			// Token: 0x04004F2A RID: 20266
			private const int skippableRegionSize = 4;
		}
		*/
	}

}
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{

    public class RegionTraverser_Patch
    {
        [ThreadStatic] public static Queue<object> freeWorkers;
        [ThreadStatic] public static int NumWorkers;
        [ThreadStatic] public static Dictionary<Region, uint[]> regionClosedIndex;

        public static void InitializeThreadStatics()
        {
            regionClosedIndex = new Dictionary<Region, uint[]>();
        }

        internal static void RunNonDestructivePatches()
        {
            Type original = typeof(RegionTraverser);
            Type patched = typeof(RegionTraverser_Patch);
            RimThreadedHarmony.Transpile(original, patched, "BreadthFirstTraverse", new Type[] {
                typeof(Region),
                typeof(RegionEntryPredicate),
                typeof(RegionProcessor),
                typeof(int),
                typeof(RegionType)
            });
            RimThreadedHarmony.Transpile(original, patched, "RecreateWorkers");

            //Verse.RegionTraverser+BFSWorker
            original = TypeByName("Verse.RegionTraverser+BFSWorker");
            patched = typeof(RegionTraverser_Patch);
            RimThreadedHarmony.Transpile(original, patched, "QueueNewOpenRegion");
            RimThreadedHarmony.Transpile(original, patched, "BreadthFirstTraverseWork");
        }
        public static IEnumerable<CodeInstruction> BreadthFirstTraverse(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            int[] matchesFound = new int[2];
            List<CodeInstruction> instructionsList = instructions.ToList();
            int i = 0;
            yield return new CodeInstruction(OpCodes.Ldsfld, Field(typeof(RegionTraverser_Patch), "freeWorkers"));
            yield return new CodeInstruction(OpCodes.Ldnull);
            yield return new CodeInstruction(OpCodes.Ceq);
            Label freeWorkersNullLabel = iLGenerator.DefineLabel();
            yield return new CodeInstruction(OpCodes.Brfalse_S, freeWorkersNullLabel);
            yield return new CodeInstruction(OpCodes.Newobj, Constructor(typeof(Queue<object>)));
            yield return new CodeInstruction(OpCodes.Stsfld, Field(typeof(RegionTraverser_Patch), "freeWorkers"));
            yield return new CodeInstruction(OpCodes.Ldc_I4_8);
            yield return new CodeInstruction(OpCodes.Stsfld, Field(typeof(RegionTraverser_Patch), "NumWorkers"));
            yield return new CodeInstruction(OpCodes.Call, Method(typeof(RegionTraverser), "RecreateWorkers"));
            instructionsList[i].labels.Add(freeWorkersNullLabel);
            while (i < instructionsList.Count)
            {
                int matchIndex = 0;
                if (
                    instructionsList[i].opcode == OpCodes.Ldsfld &&
                    (FieldInfo)instructionsList[i].operand == Field(typeof(RegionTraverser), "freeWorkers")
                )
                {
                    instructionsList[i].operand = Field(typeof(RegionTraverser_Patch), "freeWorkers");
                    matchesFound[matchIndex]++;
                }
                matchIndex++;
                if (
                    instructionsList[i].opcode == OpCodes.Ldsfld &&
                    (FieldInfo)instructionsList[i].operand == Field(typeof(RegionTraverser), "NumWorkers")
                )
                {
                    instructionsList[i].operand = Field(typeof(RegionTraverser_Patch), "NumWorkers");
                    matchesFound[matchIndex]++;
                }
                yield return instructionsList[i++];
            }
            for (int mIndex = 0; mIndex < matchesFound.Length; mIndex++)
            {
                if (matchesFound[mIndex] < 1)
                    Log.Error("IL code instruction set " + mIndex + " not found");
            }
        }
        public static IEnumerable<CodeInstruction> RecreateWorkers(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            int[] matchesFound = new int[2];
            List<CodeInstruction> instructionsList = instructions.ToList();
            int i = 0;
            yield return new CodeInstruction(OpCodes.Ldsfld, Field(typeof(RegionTraverser_Patch), "freeWorkers"));
            yield return new CodeInstruction(OpCodes.Ldnull);
            yield return new CodeInstruction(OpCodes.Ceq);
            Label freeWorkersNullLabel = iLGenerator.DefineLabel();
            yield return new CodeInstruction(OpCodes.Brfalse_S, freeWorkersNullLabel);
            yield return new CodeInstruction(OpCodes.Newobj, Constructor(typeof(Queue<object>)));
            yield return new CodeInstruction(OpCodes.Stsfld, Field(typeof(RegionTraverser_Patch), "freeWorkers"));
            yield return new CodeInstruction(OpCodes.Ldc_I4_8);
            yield return new CodeInstruction(OpCodes.Stsfld, Field(typeof(RegionTraverser_Patch), "NumWorkers"));
            instructionsList[i].labels.Add(freeWorkersNullLabel);
            while (i < instructionsList.Count)
            {
                int matchIndex = 0;
                if (
                    instructionsList[i].opcode == OpCodes.Ldsfld &&
                    (FieldInfo)instructionsList[i].operand == Field(typeof(RegionTraverser), "freeWorkers")
                )
                {
                    instructionsList[i].operand = Field(typeof(RegionTraverser_Patch), "freeWorkers");
                    matchesFound[matchIndex]++;
                }
                matchIndex++;
                if (
                    instructionsList[i].opcode == OpCodes.Ldsfld &&
                    (FieldInfo)instructionsList[i].operand == Field(typeof(RegionTraverser), "NumWorkers")
                )
                {
                    instructionsList[i].operand = Field(typeof(RegionTraverser_Patch), "NumWorkers");
                    matchesFound[matchIndex]++;
                }
                yield return instructionsList[i++];
            }
            for (int mIndex = 0; mIndex < matchesFound.Length; mIndex++)
            {
                if (matchesFound[mIndex] < 1)
                    Log.Error("IL code instruction set " + mIndex + " not found");
            }
        }

        public RegionTraverser_Patch()
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
                freeWorkers.Enqueue(new BFSWorker_Patch(closedArrayPos, this));
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
                BFSWorker_Patch bfsWorker = freeWorkers.Dequeue();
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
        public static IEnumerable<CodeInstruction> QueueNewOpenRegion(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            int[] matchesFound = new int[1];
            List<CodeInstruction> instructionsList = instructions.ToList();
            int i = 0;
            LocalBuilder regionClosedIndex = iLGenerator.DeclareLocal(typeof(uint[]));
            yield return new CodeInstruction(OpCodes.Ldarg_1);
            yield return new CodeInstruction(OpCodes.Call, Method(typeof(BFSWorker2), "getRegionClosedIndex"));
            yield return new CodeInstruction(OpCodes.Stloc, regionClosedIndex.LocalIndex);

            while (i < instructionsList.Count)
            {
                int matchIndex = 0;
                if (
                    i + 1 < instructionsList.Count &&
                    instructionsList[i + 1].opcode == OpCodes.Ldfld &&
                    (FieldInfo)instructionsList[i + 1].operand == Field(typeof(Region), "closedIndex")
                )
                {
                    instructionsList[i].opcode = OpCodes.Ldloc;
                    instructionsList[i].operand = regionClosedIndex.LocalIndex;
                    yield return instructionsList[i++];
                    i++;
                    matchesFound[matchIndex]++;
                    continue;
                }
                yield return instructionsList[i++];
            }
            for (int mIndex = 0; mIndex < matchesFound.Length; mIndex++)
            {
                if (matchesFound[mIndex] < 1)
                    Log.Error("IL code instruction set " + mIndex + " not found");
            }
        }
        public static IEnumerable<CodeInstruction> BreadthFirstTraverseWork(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            int[] matchesFound = new int[1];
            List<CodeInstruction> instructionsList = instructions.ToList();
            int i = 0;
            while (i < instructionsList.Count)
            {
                int matchIndex = 0;
                if (
                    instructionsList[i].opcode == OpCodes.Ldfld &&
                    (FieldInfo)instructionsList[i].operand == Field(typeof(Region), "closedIndex")
                )
                {
                    instructionsList[i].opcode = OpCodes.Call;
                    instructionsList[i].operand = Method(typeof(BFSWorker2), "getRegionClosedIndex");
                    matchesFound[matchIndex]++;
                }
                yield return instructionsList[i++];
            }
            for (int mIndex = 0; mIndex < matchesFound.Length; mIndex++)
            {
                if (matchesFound[mIndex] < 1)
                    Log.Error("IL code instruction set " + mIndex + " not found");
            }
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
        private RegionTraverser_Patch regionTraverser;


    }

    public class BFSWorker_Patch
    {
        public BFSWorker_Patch(int closedArrayPos, RegionTraverser_Patch regionTraverser)
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

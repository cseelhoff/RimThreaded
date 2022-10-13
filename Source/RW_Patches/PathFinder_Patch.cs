using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
using HarmonyLib;
using static HarmonyLib.AccessTools;
using System.Reflection.Emit;
using System.Reflection;
using static Verse.AI.PathFinder;
using System.Linq;

namespace RimThreaded.RW_Patches
{

    public class PathFinder_Patch
    {
        [ThreadStatic] public static List<int> disallowedCornerIndices;
        [ThreadStatic] public static PathFinderNodeFast[] calcGrid;
        [ThreadStatic] public static PriorityQueue<int, int> openList;
        [ThreadStatic] public static ushort statusOpenValue;
        [ThreadStatic] public static ushort statusClosedValue;
        [ThreadStatic] public static Dictionary<PathFinder, RegionCostCalculatorWrapper> regionCostCalculatorDict;

        public static void InitializeThreadStatics()
        {
            //openList = new FastPriorityQueue<CostNode>(new CostNodeComparer());
            openList = new PriorityQueue<int, int>();
            statusOpenValue = 1;
            statusClosedValue = 2;
            disallowedCornerIndices = new List<int>(4);
            regionCostCalculatorDict = new Dictionary<PathFinder, RegionCostCalculatorWrapper>();
        }

        internal static void AddFieldReplacements()
        {
            Dictionary<OpCode, MethodInfo> regionCostCalculatorReplacements = new Dictionary<OpCode, MethodInfo>();
            regionCostCalculatorReplacements.Add(OpCodes.Ldfld, Method(typeof(PathFinder_Patch), nameof(GetRegionCostCalculator)));
            regionCostCalculatorReplacements.Add(OpCodes.Stfld, Method(typeof(PathFinder_Patch), nameof(SetRegionCostCalculator)));
            RimThreadedHarmony.replaceFields.Add(Field(typeof(PathFinder), nameof(PathFinder.regionCostCalculator)), regionCostCalculatorReplacements);
        }

        internal static void RunNonDestructivePatches()
        {
            Type original = typeof(PathFinder);
            Type patched = typeof(PathFinder_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(InitStatusesAndPushStartNode), null, false);
        }

        public static bool InitStatusesAndPushStartNode(PathFinder __instance, ref int curIndex, IntVec3 start)
        {
            int size = __instance.mapSizeX * __instance.mapSizeZ;
            if (calcGrid == null || calcGrid.Length < size)
            {
                calcGrid = new PathFinderNodeFast[size];
            }
            return true;
        }

        public static RegionCostCalculatorWrapper GetRegionCostCalculator(PathFinder __instance)
        {
            if (!regionCostCalculatorDict.TryGetValue(__instance, out RegionCostCalculatorWrapper regionCostCalculatorWrapper))
            {
                regionCostCalculatorWrapper = new RegionCostCalculatorWrapper(__instance.map);
                regionCostCalculatorDict[__instance] = regionCostCalculatorWrapper;
            }
            return regionCostCalculatorWrapper;
        }


        public static void SetRegionCostCalculator(PathFinder __instance, RegionCostCalculatorWrapper regionCostCalculatorWrapper)
        {
            regionCostCalculatorDict[__instance] = regionCostCalculatorWrapper;
            return;
        }

        static readonly Type costNodeType = TypeByName("Verse.AI.PathFinder+CostNode");
        static readonly Type icomparerCostNodeType1 = typeof(IComparer<>).MakeGenericType(costNodeType);
        static readonly Type fastPriorityQueueCostNodeType1 = typeof(FastPriorityQueue<>).MakeGenericType(costNodeType);

        static readonly Type costNodeType2 = typeof(CostNode);
        static readonly Type icomparerCostNodeType2 = typeof(IComparer<>).MakeGenericType(costNodeType2);
        static readonly Type fastPriorityQueueCostNodeType2 = typeof(FastPriorityQueue<>).MakeGenericType(costNodeType2);

        public static IEnumerable<CodeInstruction> FindPath(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            //calcGrid
            //statusOpenValue
            //statusClosedValue
            //disallowedCornerIndices
            //openList
            //InitStatusesAndPushStartNode2
            //FinalizedPath2
            //CalculateAndAddDisallowedCorners2
            //FastPriorityQueueCostNode.get_Count
            //FastPriorityQueueCostNode.pop
            //FastPriorityQueueCostNode.push
            //FastPriorityQueueCostNode.Clear
            //CostNode.index
            //CostNode.cost
            //CostNode(int, int)
            //FastPriorityQueueCostNode(icomparerCostNode)

            int[] matchesFound = new int[14];
            List<CodeInstruction> instructionsList = instructions.ToList();
            int i = 0;
            while (i < instructionsList.Count)
            {
                int matchIndex = 0;
                if (
                    instructionsList[i].opcode == OpCodes.Ldsfld &&
                    (FieldInfo)instructionsList[i].operand == Field(typeof(PathFinder), "calcGrid")
                    )
                {
                    instructionsList[i].operand = Field(typeof(PathFinder_Patch), "calcGrid");
                    yield return instructionsList[i++];
                    matchesFound[matchIndex]++;
                    continue;
                }
                matchIndex++;

                if (
                   instructionsList[i].opcode == OpCodes.Ldsfld &&
                   (FieldInfo)instructionsList[i].operand == Field(typeof(PathFinder), "statusOpenValue")
                   )
                {
                    instructionsList[i].operand = Field(typeof(PathFinder_Patch), "statusOpenValue");
                    yield return instructionsList[i++];
                    matchesFound[matchIndex]++;
                    continue;
                }
                matchIndex++;
                if (
                   instructionsList[i].opcode == OpCodes.Ldsfld &&
                   (FieldInfo)instructionsList[i].operand == Field(typeof(PathFinder), "statusClosedValue")
                   )
                {
                    instructionsList[i].operand = Field(typeof(PathFinder_Patch), "statusClosedValue");
                    yield return instructionsList[i++];
                    matchesFound[matchIndex]++;
                    continue;
                }
                matchIndex++;
                if (
                  i + 1 < instructionsList.Count &&
                  instructionsList[i + 1].opcode == OpCodes.Ldfld &&
                  (FieldInfo)instructionsList[i + 1].operand == Field(typeof(PathFinder), "disallowedCornerIndices")
                  )
                {
                    instructionsList[i].opcode = OpCodes.Ldsfld;
                    instructionsList[i].operand = Field(typeof(PathFinder_Patch), "disallowedCornerIndices");
                    yield return instructionsList[i++];
                    i++;
                    matchesFound[matchIndex]++;
                    continue;
                }
                matchIndex++;
                if (
                    instructionsList[i].opcode == OpCodes.Ldfld &&
                    (FieldInfo)instructionsList[i].operand == Field(typeof(PathFinder), "regionCostCalculator")
                    )
                {
                    instructionsList[i].opcode = OpCodes.Call;
                    instructionsList[i].operand = Method(typeof(PathFinder_Patch), "get_regionCostCalculator");
                    yield return instructionsList[i++];
                    matchesFound[matchIndex]++;
                    continue;
                }
                if (
                    i + 1 < instructionsList.Count &&
                    instructionsList[i + 1].opcode == OpCodes.Ldfld &&
                    (FieldInfo)instructionsList[i + 1].operand == Field(typeof(PathFinder), "openList")
                    )
                {
                    instructionsList[i].opcode = OpCodes.Ldsfld;
                    instructionsList[i].operand = Field(typeof(PathFinder_Patch), "openList");
                    yield return instructionsList[i++];
                    i++;
                    matchesFound[matchIndex]++;
                    continue;
                }
                matchIndex++;
                if (
                    instructionsList[i].opcode == OpCodes.Call &&
                    (MethodInfo)instructionsList[i].operand == Method(typeof(PathFinder), "InitStatusesAndPushStartNode")
                   )
                {
                    instructionsList[i].operand = Method(typeof(PathFinder_Patch), "InitStatusesAndPushStartNode2");
                    yield return instructionsList[i++];
                    matchesFound[matchIndex]++;
                    continue;
                }
                matchIndex++;
                if (
                   instructionsList[i].opcode == OpCodes.Call &&
                   (MethodInfo)instructionsList[i].operand == Method(typeof(PathFinder), "FinalizedPath")
                   )
                {
                    instructionsList[i].operand = Method(typeof(PathFinder_Patch), "FinalizedPath2");
                    yield return instructionsList[i++];
                    matchesFound[matchIndex]++;
                    continue;
                }
                matchIndex++;
                if (
                   instructionsList[i].opcode == OpCodes.Call &&
                   (MethodInfo)instructionsList[i].operand == Method(typeof(PathFinder), "CalculateAndAddDisallowedCorners")
                   )
                {
                    instructionsList[i].operand = Method(typeof(PathFinder_Patch), "CalculateAndAddDisallowedCorners2");
                    yield return instructionsList[i++];
                    matchesFound[matchIndex]++;
                    continue;
                }
                matchIndex++;
                if (
                    instructionsList[i].opcode == OpCodes.Callvirt &&
                    (MethodInfo)instructionsList[i].operand == Method(fastPriorityQueueCostNodeType1, "get_Count")
                    )
                {
                    instructionsList[i].operand = Method(fastPriorityQueueCostNodeType2, "get_Count");
                    yield return instructionsList[i++];
                    matchesFound[matchIndex]++;
                    continue;
                }
                matchIndex++;
                if (
                    instructionsList[i].opcode == OpCodes.Callvirt &&
                    (MethodInfo)instructionsList[i].operand == Method(fastPriorityQueueCostNodeType1, "Pop")
                )
                {
                    instructionsList[i].operand = Method(fastPriorityQueueCostNodeType2, "Pop");
                    yield return instructionsList[i++];
                    matchesFound[matchIndex]++;
                    continue;
                }
                matchIndex++;
                if (
                    instructionsList[i].opcode == OpCodes.Callvirt &&
                    (MethodInfo)instructionsList[i].operand == Method(fastPriorityQueueCostNodeType1, "Push")
                )
                {
                    instructionsList[i].operand = Method(fastPriorityQueueCostNodeType2, "Push");
                    yield return instructionsList[i++];
                    matchesFound[matchIndex]++;
                    continue;
                }
                /*
				matchIndex++;
				if (
					instructionsList[i].opcode == OpCodes.Callvirt &&
					(MethodInfo)instructionsList[i].operand == Method(fastPriorityQueueCostNodeType1, "Clear")
				)
				{
					instructionsList[i].operand = Method(fastPriorityQueueCostNodeType2, "Clear");
					yield return instructionsList[i++];
					matchesFound[matchIndex]++;
					continue;
				}
				*/
                matchIndex++;
                if (
                    instructionsList[i].opcode == OpCodes.Ldfld &&
                    (FieldInfo)instructionsList[i].operand == Field(costNodeType, "index")
                )
                {
                    instructionsList[i].operand = Field(costNodeType2, "index");
                    yield return instructionsList[i++];
                    matchesFound[matchIndex]++;
                    continue;
                }
                matchIndex++;
                if (
                    instructionsList[i].opcode == OpCodes.Ldfld &&
                    (FieldInfo)instructionsList[i].operand == Field(costNodeType, "cost")
)
                {
                    instructionsList[i].operand = Field(costNodeType2, "cost");
                    yield return instructionsList[i++];
                    matchesFound[matchIndex]++;
                    continue;
                }
                matchIndex++;
                if (
                    instructionsList[i].opcode == OpCodes.Newobj &&
                    (ConstructorInfo)instructionsList[i].operand == costNodeType.GetConstructor(new Type[] { typeof(int), typeof(int) })
                )
                {
                    instructionsList[i].operand = costNodeType2.GetConstructor(new Type[] { typeof(int), typeof(int) });
                    yield return instructionsList[i++];
                    matchesFound[matchIndex]++;
                    continue;
                }
                matchIndex++;
                if (
                    instructionsList[i].opcode == OpCodes.Newobj &&
                    (ConstructorInfo)instructionsList[i].operand == fastPriorityQueueCostNodeType1.GetConstructor(new Type[] { icomparerCostNodeType1 })
                )
                {
                    instructionsList[i].operand = fastPriorityQueueCostNodeType2.GetConstructor(new Type[] { icomparerCostNodeType2 });
                    yield return instructionsList[i++];
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

    }
}

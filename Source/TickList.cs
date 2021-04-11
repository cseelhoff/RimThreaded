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
    public class TickList_Patch
    {
        public static FieldRef<TickList, List<List<Thing>>> thingLists =
            FieldRefAccess<TickList, List<List<Thing>>>("thingLists");
        public static FieldRef<TickList, TickerType> tickType =
            FieldRefAccess<TickList, TickerType>("tickType");


        private static readonly MethodInfo methodGetTickInterval =
            Method(typeof(TickList), "get_TickInterval");
        private static readonly Func<TickList, int> funcGetTickInterval =
            (Func<TickList, int>)Delegate.CreateDelegate(typeof(Func<TickList, int>), methodGetTickInterval);

        public static void RunNonDestructivePatches()
        {
            Type original = typeof(TickList);
            Type patched = typeof(TickList_Patch);
            RimThreadedHarmony.TranspileMethodLock(original, "RegisterThing");
            RimThreadedHarmony.TranspileMethodLock(original, "DeregisterThing");
            RimThreadedHarmony.Transpile(original, patched, "Tick");
            RimThreadedHarmony.Postfix(original, patched, "Tick", "TickPostfix");
        }


        public static void TickPostfix(TickList __instance)
        {
            int currentTickInterval = funcGetTickInterval(__instance);
            TickerType currentTickType = tickType(__instance);
            switch (currentTickType)
            {
                case TickerType.Normal:
                    RimThreaded.thingListNormal = thingLists(__instance)[Find.TickManager.TicksGame % currentTickInterval];
                    RimThreaded.thingListNormalTicks = RimThreaded.thingListNormal.Count;
                    break;
                case TickerType.Rare:
                    RimThreaded.thingListRare = thingLists(__instance)[Find.TickManager.TicksGame % currentTickInterval];
                    RimThreaded.thingListRareTicks = RimThreaded.thingListRare.Count;
                    break;
                case TickerType.Long:
                    RimThreaded.thingListLong = thingLists(__instance)[Find.TickManager.TicksGame % currentTickInterval];
                    RimThreaded.thingListLongTicks = RimThreaded.thingListLong.Count;
                    break;
            }
        }

        public static IEnumerable<CodeInstruction> Tick(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            List<CodeInstruction> instructionsList = instructions.ToList();
            int i = 0;
            while (i < instructionsList.Count)
            {
                if (i + 2 < instructionsList.Count && instructionsList[i + 2].opcode == OpCodes.Call)
                {
                    if (instructionsList[i + 2].operand is MethodInfo methodInfo)
                    {
                        if (methodInfo == Method(typeof(Find), "get_TickManager"))
                        {
                            List<Label> labels = instructionsList[i].labels;
                            while (i < instructionsList.Count)
                            {
                                if (instructionsList[i].opcode == OpCodes.Blt)
                                {
                                    i++;
                                    foreach (Label label in labels)
                                    {
                                        instructionsList[i].labels.Add(label);
                                    }
                                    break;
                                }
                                i++;
                            }

                        }
                    }
                }
                yield return instructionsList[i++];
            }
        }

    }
}

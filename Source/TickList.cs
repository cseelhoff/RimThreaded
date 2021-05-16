using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
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
        public static readonly Func<TickList, int> funcGetTickInterval =
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
                case TickerType.Never:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
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

        public static List<Thing> normalThingList;
        public static int normalThingListTicks;

        public static void NormalThingPrepare()
        {
            TickList tickList = TickManager_Patch.tickListNormal(RimThreaded.callingTickManager);
            tickList.Tick();
            int currentTickInterval = funcGetTickInterval(tickList);
            normalThingList = thingLists(tickList)[RimThreaded.callingTickManager.TicksGame % currentTickInterval];
            normalThingListTicks = normalThingList.Count;
        }

        public static void NormalThingTick()
        {
            while (true)
            {
                int index = Interlocked.Decrement(ref normalThingListTicks);
                if (index < 0) return;
                Thing thing = normalThingList[index];
                if (thing.Destroyed) continue;
                try
                {
                    thing.Tick();
                }
                catch (Exception ex)
                {
                    string text = thing.Spawned ? (" (at " + thing.Position + ")") : "";
                    if (Prefs.DevMode)
                    {
                        Log.Error("Exception ticking " + thing.ToStringSafe() + text + ": " + ex);
                    }
                    else
                    {
                        Log.ErrorOnce(
                            "Exception ticking " + thing.ToStringSafe() + text +
                            ". Suppressing further errors. Exception: " + ex, thing.thingIDNumber ^ 0x22627165);
                    }
                }
            }
        }

        public static List<Thing> rareThingList;
        public static int rareThingListTicks;

        public static void RareThingPrepare()
        {
            TickList tickList = TickManager_Patch.tickListRare(RimThreaded.callingTickManager);
            tickList.Tick();
            int currentTickInterval = funcGetTickInterval(tickList);
            rareThingList = thingLists(tickList)[RimThreaded.callingTickManager.TicksGame % currentTickInterval];
            rareThingListTicks = rareThingList.Count;
        }

        public static void RareThingTick()
        {
            while (true)
            {
                int index = Interlocked.Decrement(ref rareThingListTicks);
                if (index < 0) return;
                Thing thing = rareThingList[index];
                if (thing.Destroyed) continue;
                try
                {
                    thing.TickRare();
                }
                catch (Exception ex)
                {
                    string text = thing.Spawned ? (" (at " + thing.Position + ")") : "";
                    if (Prefs.DevMode)
                    {
                        Log.Error("Exception ticking " + thing.ToStringSafe() + text + ": " + ex);
                    }
                    else
                    {
                        Log.ErrorOnce(
                            "Exception ticking " + thing.ToStringSafe() + text +
                            ". Suppressing further errors. Exception: " + ex, thing.thingIDNumber ^ 0x22627165);
                    }
                }
            }
        }

        public static List<Thing> longThingList;
        public static int longThingListTicks;

        public static void LongThingPrepare()
        {
            TickList tickList = TickManager_Patch.tickListLong(RimThreaded.callingTickManager);
            tickList.Tick();
            int currentTickInterval = funcGetTickInterval(tickList);
            longThingList = thingLists(tickList)[RimThreaded.callingTickManager.TicksGame % currentTickInterval];
            longThingListTicks = longThingList.Count;
        }

        public static void LongThingTick()
        {
            while (true)
            {
                int index = Interlocked.Decrement(ref longThingListTicks);
                if (index < 0) return;
                Thing thing = longThingList[index];
                if (thing.Destroyed) continue;
                try
                {
                    thing.TickLong();
                }
                catch (Exception ex)
                {
                    string text = thing.Spawned ? (" (at " + thing.Position + ")") : "";
                    if (Prefs.DevMode)
                    {
                        Log.Error("Exception ticking " + thing.ToStringSafe() + text + ": " + ex);
                    }
                    else
                    {
                        Log.ErrorOnce(
                            "Exception ticking " + thing.ToStringSafe() + text +
                            ". Suppressing further errors. Exception: " + ex, thing.thingIDNumber ^ 0x22627165);
                    }
                }
            }
        }

    }
}

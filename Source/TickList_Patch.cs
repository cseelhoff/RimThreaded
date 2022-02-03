using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Verse;
using Verse.AI;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{
    public class TickList_Patch
    {

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
            int currentTickInterval = __instance.TickInterval;
            TickerType currentTickType = __instance.tickType;
            switch (currentTickType)
            {
                case TickerType.Normal:
                    RimThreaded.thingListNormal = __instance.thingLists[Find.TickManager.TicksGame % currentTickInterval];
                    RimThreaded.thingListNormalTicks = RimThreaded.thingListNormal.Count;
                    break;
                case TickerType.Rare:
                    RimThreaded.thingListRare = __instance.thingLists[Find.TickManager.TicksGame % currentTickInterval];
                    RimThreaded.thingListRareTicks = RimThreaded.thingListRare.Count;
                    break;
                case TickerType.Long:
                    RimThreaded.thingListLong = __instance.thingLists[Find.TickManager.TicksGame % currentTickInterval];
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
            TickList tickList = RimThreaded.callingTickManager.tickListNormal;
            tickList.Tick();
            int currentTickInterval = tickList.TickInterval;
            normalThingList = tickList.thingLists[RimThreaded.callingTickManager.TicksGame % currentTickInterval];
            normalThingListTicks = normalThingList.Count;
        }

        [ThreadStatic]
        static Stopwatch s1;
        public static void NormalThingTick()
        {
            if (s1 == null)
                s1 = new Stopwatch();
            while (true)
            {
                int index = Interlocked.Decrement(ref normalThingListTicks);
                if (index < 0) return;
                Thing thing = normalThingList[index];
                if (thing.Destroyed) continue;
                try
                {
                    s1.Restart();
                    if (thing.ToString().Equals("McDowell") && Find.TickManager.TicksAbs == 26634151)
                    {
                        Log.Message("McDowell Debug " + Find.TickManager.TicksAbs.ToString());
                        Pawn pawn = thing as Pawn;
                        if (DebugSettings.noAnimals && pawn.Spawned && pawn.RaceProps.Animal)
                        {
                            pawn.Destroy();
                            return;
                        }
                        if (Find.TickManager.TicksGame % 250 == 0)
                        {
                            pawn.TickRare();
                        }
                        bool suspended = pawn.Suspended;
                        if (!suspended)
                        {
                            if (pawn.Spawned)
                            {
                                pawn.pather.PatherTick();
                            }
                            if (pawn.Spawned)
                            {
                                pawn.stances.StanceTrackerTick();
                                pawn.verbTracker.VerbsTick();
                            }
                            if (pawn.Spawned)
                            {
                                pawn.roping.RopingTick();
                                pawn.natives.NativeVerbsTick();
                            }
                            if (pawn.Spawned)
                            {
                                pawn.jobs.JobTrackerTick();
                            }
                            if (pawn.Spawned)
                            {
                                pawn.Drawer.DrawTrackerTick();
                                pawn.rotationTracker.RotationTrackerTick();
                            }
                            pawn.health.HealthTick();
                            if (!pawn.Dead)
                            {
                                pawn.mindState.MindStateTick();
                                pawn.carryTracker.CarryHandsTick();
                            }
                        }
                        if (!pawn.Dead)
                        {
                            pawn.needs.NeedsTrackerTick();
                        }
                        if (!suspended)
                        {
                            if (pawn.equipment != null)
                            {
                                pawn.equipment.EquipmentTrackerTick();
                            }
                            if (pawn.apparel != null)
                            {
                                pawn.apparel.ApparelTrackerTick();
                            }
                            if (pawn.interactions != null && pawn.Spawned)
                            {
                                pawn.interactions.InteractionsTrackerTick();
                            }
                            if (pawn.caller != null)
                            {
                                pawn.caller.CallTrackerTick();
                            }
                            if (pawn.skills != null)
                            {
                                pawn.skills.SkillsTick();
                            }
                            if (pawn.abilities != null)
                            {
                                pawn.abilities.AbilitiesTick();
                            }
                            if (pawn.inventory != null)
                            {
                                pawn.inventory.InventoryTrackerTick();
                            }
                            if (pawn.drafter != null)
                            {
                                pawn.drafter.DraftControllerTick();
                            }
                            if (pawn.relations != null)
                            {
                                pawn.relations.RelationsTrackerTick();
                            }
                            if (ModsConfig.RoyaltyActive && pawn.psychicEntropy != null)
                            {
                                pawn.psychicEntropy.PsychicEntropyTrackerTick();
                            }
                            if (pawn.RaceProps.Humanlike)
                            {
                                pawn.guest.GuestTrackerTick();
                            }
                            if (pawn.ideo != null)
                            {
                                pawn.ideo.IdeoTrackerTick();
                            }
                            if (pawn.royalty != null && ModsConfig.RoyaltyActive)
                            {
                                pawn.royalty.RoyaltyTrackerTick();
                            }
                            if (pawn.style != null && ModsConfig.IdeologyActive)
                            {
                                pawn.style.StyleTrackerTick();
                            }
                            if (pawn.styleObserver != null && ModsConfig.IdeologyActive)
                            {
                                pawn.styleObserver.StyleObserverTick();
                            }
                            if (pawn.surroundings != null && ModsConfig.IdeologyActive)
                            {
                                pawn.surroundings.SurroundingsTrackerTick();
                            }
                            pawn.ageTracker.AgeTick();
                            pawn.records.RecordsTick();
                        }
                        pawn.guilt?.GuiltTrackerTick();
                    }
                    else 
                        thing.Tick();
                    long milli2 = s1.ElapsedMilliseconds;
                    if (Prefs.LogVerbose && milli2 > 100)
                    {
                        Log.Warning(thing.ToString() + " tick was " + milli2.ToString() + "ms tickabs:" + Find.TickManager.TicksAbs.ToString());
                    }
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
            TickList tickList = RimThreaded.callingTickManager.tickListRare;
            tickList.Tick();
            int currentTickInterval = tickList.TickInterval;
            rareThingList = tickList.thingLists[RimThreaded.callingTickManager.TicksGame % currentTickInterval];
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
            TickList tickList = RimThreaded.callingTickManager.tickListLong;
            tickList.Tick();
            int currentTickInterval = tickList.TickInterval;
            longThingList = tickList.thingLists[RimThreaded.callingTickManager.TicksGame % currentTickInterval];
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

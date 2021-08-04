using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld.Planet;
using System.Threading;
using RimWorld;

namespace RimThreaded
{
    //Class was largely overhauled to allow multithreaded ticking for WorldPawns.Tick()
    public class WorldPawns_Patch
    {
        [ThreadStatic] public static List<Pawn> tmpPawnsToRemove;
        
        internal static void InitializeThreadStatics()
        {
            tmpPawnsToRemove = new List<Pawn>();
        }

        internal static void RunDestructivePatches()
        {
            Type original = typeof(WorldPawns);
            Type patched = typeof(WorldPawns_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(WorldPawnsTick));
            RimThreadedHarmony.Prefix(original, patched, nameof(get_AllPawnsAlive));
            RimThreadedHarmony.Prefix(original, patched, nameof(AddPawn));
            RimThreadedHarmony.Prefix(original, patched, nameof(RemovePawn));
            RimThreadedHarmony.Prefix(original, patched, nameof(Notify_PawnDestroyed));
        }
        public static bool Notify_PawnDestroyed(WorldPawns __instance, Pawn p)
        {
            lock (__instance)
            {
                if (!__instance.pawnsAlive.Contains(p) && !__instance.pawnsMothballed.Contains(p))
                    return false;

                HashSet<Pawn> newPawnsAlive = new HashSet<Pawn>(__instance.pawnsAlive);
                newPawnsAlive.Remove(p);
                __instance.pawnsAlive = newPawnsAlive;

                HashSet<Pawn> newPawnsMothballed = new HashSet<Pawn>(__instance.pawnsMothballed);
                newPawnsMothballed.Remove(p);
                __instance.pawnsMothballed = newPawnsMothballed;

                __instance.pawnsDead.Add(p);
            }
            return false;
        }
        public static bool RemovePawn(WorldPawns __instance, Pawn p)
        {
            lock (__instance)
            {
                if (!__instance.Contains(p))
                    Log.Error("Tried to remove pawn " + (object)p + " from " + (object)__instance.GetType() + ", but it's not here.");
                __instance.gc.CancelGCPass();
                if (__instance.pawnsMothballed.Contains(p))
                {
                    if (Find.TickManager.TicksGame % 15000 != 0)
                    {
                        try
                        {
                            p.TickMothballed(Find.TickManager.TicksGame % 15000);
                        }
                        catch (Exception ex)
                        {
                            Log.Error("Exception ticking mothballed world pawn (just before removing): " + (object)ex);
                        }
                    }
                }
                HashSet<Pawn> newPawnsAlive = new HashSet<Pawn>(__instance.pawnsAlive);
                newPawnsAlive.Remove(p);
                __instance.pawnsAlive = newPawnsAlive;

                HashSet<Pawn> newPawnsMothballed = new HashSet<Pawn>(__instance.pawnsMothballed);
                newPawnsMothballed.Remove(p);
                __instance.pawnsMothballed = newPawnsMothballed;

                HashSet<Pawn> newPawnsDead = new HashSet<Pawn>(__instance.pawnsDead);
                newPawnsDead.Remove(p);
                __instance.pawnsDead = newPawnsDead;

                HashSet<Pawn> newPawnsForcefullyKeptAsWorldPawns = new HashSet<Pawn>(__instance.pawnsForcefullyKeptAsWorldPawns);
                newPawnsDead.Remove(p);
                __instance.pawnsForcefullyKeptAsWorldPawns = newPawnsForcefullyKeptAsWorldPawns;

                p.becameWorldPawnTickAbs = -1;
            }
            return false;
        }
        public static bool AddPawn(WorldPawns __instance, Pawn p)
        {
            lock (__instance)
            {
                __instance.gc.CancelGCPass();
                if (p.Dead || p.Destroyed)
                {
                    __instance.pawnsDead.Add(p);
                }
                else
                {
                    try
                    {
                        int num = 0;
                        while (__instance.ShouldAutoTendTo(p) && num < 30)
                        {
                            TendUtility.DoTend(null, p, null);
                            num++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.ErrorOnce("Exception tending to a world pawn " + p.ToStringSafe() + ". Suppressing further errors. " + ex, p.thingIDNumber ^ 0x85C154);
                    }
                    __instance.pawnsAlive.Add(p);
                }
                p.Notify_PassedToWorld();
            }
            return false;
        }

        //ThreadSafety: Return a new list, instead of an instance field (allPawnsAliveResult)
        public static bool get_AllPawnsAlive(WorldPawns __instance, ref List<Pawn> __result)
        {
            List<Pawn> newAllPawnsAliveResult;
            newAllPawnsAliveResult = new List<Pawn>(__instance.pawnsAlive);
            newAllPawnsAliveResult.AddRange(__instance.pawnsMothballed);
            __instance.allPawnsAliveResult = newAllPawnsAliveResult;
            __result = newAllPawnsAliveResult;
            return false;
        }

        private static void DoMothballProcessing(WorldPawns __instance)
        {
            Pawn[] pawnArray;
            lock (__instance.pawnsMothballed)
            {
                pawnArray = __instance.pawnsMothballed.ToArray();
            }
            Pawn p;
            for (int index=0; index< pawnArray.Length; index++)
            {
                p = pawnArray[index];
                try
                {                    
                    p.TickMothballed(15000);
                }
                catch (Exception ex)
                {
                    Log.ErrorOnce("Exception ticking mothballed world pawn. Suppressing further errors. " + ex, p.thingIDNumber ^ 1535437893);
                }
            }
            lock (__instance.pawnsAlive)
            {
                pawnArray = __instance.pawnsAlive.ToArray();
            }
            for (int index = 0; index < pawnArray.Length; index++)
            {
                p = pawnArray[index];
                if (__instance.ShouldMothball(p))
                {
                    lock (__instance)
                    {
                        HashSet<Pawn> newSet = new HashSet<Pawn>(__instance.pawnsAlive);
                        newSet.Remove(p);
                        __instance.pawnsAlive = newSet;
                    }
                    lock (__instance.pawnsMothballed)
                    {
                        __instance.pawnsMothballed.Add(p);
                    }
                }
            }
        }

        public static bool WorldPawnsTick(WorldPawns __instance)
        {
            worldPawnsAlive = __instance.pawnsAlive.ToList();
            worldPawnsTicks = worldPawnsAlive.Count;

            if (Find.TickManager.TicksGame % 15000 == 0)
                DoMothballProcessing(__instance);

            tmpPawnsToRemove.Clear();
            foreach (Pawn pawn in __instance.pawnsDead)
            {
                if (pawn == null)
                {
                    Log.ErrorOnce("Dead null world pawn detected, discarding.", 94424128);
                    tmpPawnsToRemove.Add(pawn);
                }
                else if (pawn.Discarded)
                {
                    Log.Error("World pawn " + pawn + " has been discarded while still being a world pawn. This should never happen, because discard destroy mode means that the pawn is no longer managed by anything. Pawn should have been removed from the world first.");
                    tmpPawnsToRemove.Add(pawn);
                }
            }
            lock (__instance)
            {
                HashSet<Pawn> newSet = new HashSet<Pawn>(__instance.pawnsDead);
                foreach (Pawn p in tmpPawnsToRemove)
                    newSet.Remove(p);
                __instance.pawnsDead = newSet;
            }
            try
            {
                __instance.gc.WorldPawnGCTick();
            }
            catch (Exception ex)
            {
                Log.Error("Error in WorldPawnGCTick(): " + ex);
            }
            
            return false;
        }

        public static List<Pawn> worldPawnsAlive;
        public static int worldPawnsTicks;

        public static void WorldPawnsPrepare()
        {
            try
            {
                World world = Find.World;
                world.worldPawns.WorldPawnsTick();
            }
            catch (Exception ex3)
            {
                Log.Error(ex3.ToString());
            }
        }

        //candidate for reverse patch
        public static void WorldPawnsListTick()
        {
            while (true)
            {
                int index = Interlocked.Decrement(ref worldPawnsTicks);
                if (index < 0) return;
                Pawn pawn = worldPawnsAlive[index];
                try
                {
                    pawn.Tick();
                }
                catch (Exception ex)
                {
                    Log.ErrorOnce("Exception ticking world pawn " + pawn.ToStringSafe() + ". Suppressing further errors. " + ex, pawn.thingIDNumber ^ 1148571423);
                }
                try
                {
                    if (!pawn.Dead && !pawn.Destroyed && (pawn.IsHashIntervalTick(7500) && !pawn.IsCaravanMember()) && !PawnUtility.IsTravelingInTransportPodWorldObject(pawn))
                        TendUtility.DoTend(null, pawn, null);
                }
                catch (Exception ex)
                {
                    Log.ErrorOnce("Exception tending to a world pawn " + pawn.ToStringSafe() + ". Suppressing further errors. " + ex, pawn.thingIDNumber ^ 8765780);
                }
            }
        }
    }
}

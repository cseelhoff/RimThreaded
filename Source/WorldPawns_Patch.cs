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
            RimThreadedHarmony.Prefix(original, patched, "WorldPawnsTick");
            RimThreadedHarmony.Prefix(original, patched, "get_AllPawnsAlive");
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
                    Log.ErrorOnce("Exception ticking mothballed world pawn. Suppressing further errors. " + ex, p.thingIDNumber ^ 1535437893, false);
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
                    Log.ErrorOnce("Dead null world pawn detected, discarding.", 94424128, false);
                    tmpPawnsToRemove.Add(pawn);
                }
                else if (pawn.Discarded)
                {
                    Log.Error("World pawn " + pawn + " has been discarded while still being a world pawn. This should never happen, because discard destroy mode means that the pawn is no longer managed by anything. Pawn should have been removed from the world first.", false);
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
                Log.Error("Error in WorldPawnGCTick(): " + ex, false);
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
                    Log.ErrorOnce("Exception ticking world pawn " + pawn.ToStringSafe() + ". Suppressing further errors. " + ex, pawn.thingIDNumber ^ 1148571423, false);
                }
                try
                {
                    if (!pawn.Dead && !pawn.Destroyed && (pawn.IsHashIntervalTick(7500) && !pawn.IsCaravanMember()) && !PawnUtility.IsTravelingInTransportPodWorldObject(pawn))
                        TendUtility.DoTend(null, pawn, null);
                }
                catch (Exception ex)
                {
                    Log.ErrorOnce("Exception tending to a world pawn " + pawn.ToStringSafe() + ". Suppressing further errors. " + ex, pawn.thingIDNumber ^ 8765780, false);
                }
            }
        }
    }
}

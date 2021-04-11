using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld.Planet;
using System.Reflection;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{

    public class WorldPawns_Patch
    {
        [ThreadStatic] public static List<Pawn> tmpPawnsToRemove;

        public static FieldRef<WorldPawns, List<Pawn>> allPawnsAliveResult = FieldRefAccess<WorldPawns, List<Pawn>>("allPawnsAliveResult");
        public static FieldRef<WorldPawns, HashSet<Pawn>> pawnsAlive = FieldRefAccess<WorldPawns, HashSet<Pawn>>("pawnsAlive");
        public static FieldRef<WorldPawns, HashSet<Pawn>> pawnsMothballed = FieldRefAccess<WorldPawns, HashSet<Pawn>>("pawnsMothballed");
        public static FieldRef<WorldPawns, HashSet<Pawn>> pawnsDead = FieldRefAccess<WorldPawns, HashSet<Pawn>>("pawnsDead");

        private static readonly MethodInfo methodShouldMothball =
            Method(typeof(WorldPawns), "ShouldMothball", new Type[] { typeof(Pawn) });
        private static readonly Func<WorldPawns, Pawn, bool> funcShouldMothball =
            (Func<WorldPawns, Pawn, bool>)Delegate.CreateDelegate(typeof(Func<WorldPawns, Pawn, bool>), methodShouldMothball);
        public static void InitializeThreadStatics()
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

        public static bool get_AllPawnsAlive(WorldPawns __instance, ref List<Pawn> __result)
        {
            List<Pawn> newAllPawnsAliveResult;
            lock (__instance)
            {
                newAllPawnsAliveResult = new List<Pawn>(pawnsAlive(__instance));
                newAllPawnsAliveResult.AddRange(pawnsMothballed(__instance));
                allPawnsAliveResult(__instance) = newAllPawnsAliveResult;
            }
            __result = newAllPawnsAliveResult;
            return false;
        }

        private static void DoMothballProcessing(WorldPawns __instance)
        {
            Pawn[] pawnArray;
            lock (pawnsMothballed(__instance))
            {
                pawnArray = pawnsMothballed(__instance).ToArray();
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
            lock (pawnsAlive(__instance))
            {
                pawnArray = pawnsAlive(__instance).ToArray();
            }
            for (int index = 0; index < pawnArray.Length; index++)
            {
                p = pawnArray[index];
                if (funcShouldMothball(__instance, p))
                {
                    lock (__instance)
                    {
                        HashSet<Pawn> newSet = new HashSet<Pawn>(pawnsAlive(__instance));
                        newSet.Remove(p);
                        pawnsAlive(__instance) = newSet;
                    }
                    lock (pawnsMothballed(__instance))
                    {
                        pawnsMothballed(__instance).Add(p);
                    }
                }
            }
        }

        public static bool WorldPawnsTick(WorldPawns __instance)
        {
            RimThreaded.worldPawnsAlive = pawnsAlive(__instance).ToList();
            RimThreaded.worldPawnsTicks = pawnsAlive(__instance).Count;

            if (Find.TickManager.TicksGame % 15000 == 0)
                DoMothballProcessing(__instance);

            tmpPawnsToRemove.Clear();
            foreach (Pawn pawn in pawnsDead(__instance))
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
                HashSet<Pawn> newSet = new HashSet<Pawn>(pawnsDead(__instance));
                foreach (Pawn p in tmpPawnsToRemove)
                    newSet.Remove(p);
                pawnsDead(__instance) = newSet;
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

    }
}

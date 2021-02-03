using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using RimWorld.Planet;
using System.Collections.Concurrent;

namespace RimThreaded
{

    public class WorldPawns_Patch
    {
        public static AccessTools.FieldRef<WorldPawns, List<Pawn>> allPawnsAliveResult =
            AccessTools.FieldRefAccess<WorldPawns, List<Pawn>>("allPawnsAliveResult");
        public static AccessTools.FieldRef<WorldPawns, HashSet<Pawn>> pawnsAlive =
            AccessTools.FieldRefAccess<WorldPawns, HashSet<Pawn>>("pawnsAlive");
        public static AccessTools.FieldRef<WorldPawns, HashSet<Pawn>> pawnsMothballed =
            AccessTools.FieldRefAccess<WorldPawns, HashSet<Pawn>>("pawnsMothballed");
        //public static AccessTools.FieldRef<WorldPawns, List<Pawn>> tmpPawnsToRemove =
            //AccessTools.FieldRefAccess<WorldPawns, List<Pawn>>("tmpPawnsToRemove");
        //public static AccessTools.FieldRef<WorldPawns, List<Pawn>> tmpPawnsToTick =
            //AccessTools.FieldRefAccess<WorldPawns, List<Pawn>>("tmpPawnsToTick");
        public static AccessTools.FieldRef<WorldPawns, HashSet<Pawn>> pawnsDead =
            AccessTools.FieldRefAccess<WorldPawns, HashSet<Pawn>>("pawnsDead");

        private static HediffDef DefPreventingMothball(Pawn p)
        {
            List<Hediff> hediffs = p.health.hediffSet.hediffs;
            Hediff hediff;
            for (int index = 0; index < hediffs.Count; ++index)
            {
                try
                {
                    hediff = hediffs[index];
                } catch (IndexOutOfRangeException) { break; }
                if (!hediff.def.AlwaysAllowMothball && !hediff.IsPermanent())
                    return hediff.def;
            }
            return null;
        }
        private static bool ShouldMothball(Pawn p)
        {
            return DefPreventingMothball(p) == null && !p.IsCaravanMember() && !PawnUtility.IsTravelingInTransportPodWorldObject(p);
        }
        public static bool get_AllPawnsAlive(WorldPawns __instance, ref List<Pawn> __result)
        {
            lock (allPawnsAliveResult(__instance))
            {
                allPawnsAliveResult(__instance).Clear();
                allPawnsAliveResult(__instance).AddRange(pawnsAlive(__instance));
                allPawnsAliveResult(__instance).AddRange(pawnsMothballed(__instance));
            }
            __result = allPawnsAliveResult(__instance);
            return false;
        }

        private static void DoMothballProcessing(WorldPawns __instance)
        {

            //List<Pawn> tmpPawnsToTick = new List<Pawn>(pawnsMothballed(__instance));
            //tmpPawnsToTick(__instance).Clear();
            //tmpPawnsToTick(__instance).AddRange((IEnumerable<Pawn>)pawnsMothballed(__instance));
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
                    Log.ErrorOnce("Exception ticking mothballed world pawn. Suppressing further errors. " + (object)ex, p.thingIDNumber ^ 1535437893, false);
                }
            }
            //tmpPawnsToTick(__instance).Clear();
            //tmpPawnsToTick(__instance).AddRange((IEnumerable<Pawn>)pawnsAlive(__instance));
            lock (pawnsAlive(__instance))
            {
                pawnArray = pawnsAlive(__instance).ToArray();
            }
            for (int index = 0; index < pawnArray.Length; index++)
            {
                p = pawnArray[index];
                if (ShouldMothball(p))
                {
                    lock (pawnsAlive(__instance))
                    {
                        pawnsAlive(__instance).Remove(p);
                    }
                    lock (pawnsMothballed(__instance))
                    {
                        pawnsMothballed(__instance).Add(p);
                    }
                }
            }
            //tmpPawnsToTick(__instance).Clear();
        }

        public static bool WorldPawnsTick(WorldPawns __instance)
        {
            //RimThreaded.tmpPawnsToTick.Clear();
            //RimThreaded.tmpPawnsToTick.AddRange(pawnsAlive(__instance));
            //RimThreaded.worldPawns = __instance;
            //RimThreaded.tmpPawnsToTick = new ConcurrentQueue<Pawn>(pawnsAlive(__instance));
            //RimThreaded.worldPawns = __instance;
            RimThreaded.worldPawnsAlive = pawnsAlive(__instance).ToList();
            RimThreaded.worldPawnsTicks = pawnsAlive(__instance).Count;
            //RimThreaded.MainThreadWaitLoop();
            /*
            for (int index = 0; index < WorldPawns.tmpPawnsToTick.Count; ++index)
            {
                try
                {
                    WorldPawns.tmpPawnsToTick[index].Tick();
                }
                catch (Exception ex)
                {
                    Log.ErrorOnce("Exception ticking world pawn " + WorldPawns.tmpPawnsToTick[index].ToStringSafe<Pawn>() + ". Suppressing further errors. " + (object)ex, WorldPawns.tmpPawnsToTick[index].thingIDNumber ^ 1148571423, false);
                }
                try
                {
                    if (this.ShouldAutoTendTo(WorldPawns.tmpPawnsToTick[index]))
                        TendUtility.DoTend((Pawn)null, WorldPawns.tmpPawnsToTick[index], (Medicine)null);
                }
                catch (Exception ex)
                {
                    Log.ErrorOnce("Exception tending to a world pawn " + WorldPawns.tmpPawnsToTick[index].ToStringSafe<Pawn>() + ". Suppressing further errors. " + (object)ex, WorldPawns.tmpPawnsToTick[index].thingIDNumber ^ 8765780, false);
                }
            }
            */
            //WorldPawns.tmpPawnsToTick.Clear();
            if (Find.TickManager.TicksGame % 15000 == 0)
                DoMothballProcessing(__instance);
            //tmpPawnsToRemove(__instance).Clear();
            List<Pawn> tmpPawnsToRemove = new List<Pawn>();
            foreach (Pawn pawn in pawnsDead(__instance))
            {
                if (pawn == null)
                {
                    Log.ErrorOnce("Dead null world pawn detected, discarding.", 94424128, false);
                    tmpPawnsToRemove.Add(pawn);
                }
                else if (pawn.Discarded)
                {
                    Log.Error("World pawn " + (object)pawn + " has been discarded while still being a world pawn. This should never happen, because discard destroy mode means that the pawn is no longer managed by anything. Pawn should have been removed from the world first.", false);
                    tmpPawnsToRemove.Add(pawn);
                }
            }
            foreach (Pawn p in tmpPawnsToRemove)
                pawnsDead(__instance).Remove(p);
            //tmpPawnsToRemove(__instance).Clear();
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

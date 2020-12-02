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
using Verse.AI.Group;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{

    public class Pawn_Patch
    {

        public static bool Destroy(Pawn __instance, DestroyMode mode = DestroyMode.Vanish)
        {
            if (mode != 0 && mode != DestroyMode.KillFinalize)
            {
                Log.Error(string.Concat("Destroyed pawn ", __instance, " with unsupported mode ", mode, "."));
            }
            //ThingWithComps twc = __instance;
            //twc.Destroy(mode);
            ThingWithCompsDestroy(__instance, mode);
            Find.WorldPawns.Notify_PawnDestroyed(__instance);
            if (__instance.ownership != null)
            {
                Building_Grave assignedGrave = __instance.ownership.AssignedGrave;
                __instance.ownership.UnclaimAll();
                if (mode == DestroyMode.KillFinalize)
                {
                    assignedGrave?.CompAssignableToPawn.TryAssignPawn(__instance);
                }
            }

            __instance.ClearMind(ifLayingKeepLaying: false, clearInspiration: true);
            Lord lord = __instance.GetLord();
            if (lord != null)
            {
                PawnLostCondition cond = (mode != DestroyMode.KillFinalize) ? PawnLostCondition.Vanished : PawnLostCondition.IncappedOrKilled;
                lord.Notify_PawnLost(__instance, cond);
            }

            if (Current.ProgramState == ProgramState.Playing)
            {
                Find.GameEnder.CheckOrUpdateGameOver();
                Find.TaleManager.Notify_PawnDestroyed(__instance);
            }

            //foreach (Pawn item in PawnsFinder.AllMapsWorldAndTemporary_Alive.Where((Pawn p) => p.playerSettings != null && p.playerSettings.Master == __instance))
            //{
            //item.playerSettings.Master = null;
            //}
            for (int i = 0; i < PawnsFinder.AllMapsWorldAndTemporary_Alive.Count; i++)
            {
                Pawn p;
                try
                {
                    p = PawnsFinder.AllMapsWorldAndTemporary_Alive[i];
                }
                catch (ArgumentOutOfRangeException)
                {
                    break;
                }
                if (p.playerSettings != null && p.playerSettings.Master == __instance)
                {
                    p.playerSettings.Master = null;
                }
            }

            if (__instance.equipment != null)
            {
                __instance.equipment.Notify_PawnDied();
            }

            if (mode != DestroyMode.KillFinalize)
            {
                if (__instance.equipment != null)
                {
                    __instance.equipment.DestroyAllEquipment();
                }

                __instance.inventory.DestroyAll();
                if (__instance.apparel != null)
                {
                    __instance.apparel.DestroyAll();
                }
            }

            WorldPawns worldPawns = Find.WorldPawns;
            if (!worldPawns.IsBeingDiscarded(__instance) && !worldPawns.Contains(__instance))
            {
                worldPawns.PassToWorld(__instance);
            }
            return false;
        }



        public static FieldRef<ThingWithComps, List<ThingComp>> comps = FieldRefAccess<ThingWithComps, List<ThingComp>>("comps");

        private static void ThingWithCompsDestroy(ThingWithComps __instance, DestroyMode mode = DestroyMode.Vanish)
        {
            Map map = __instance.Map;
            ThingDestroy(__instance, mode);
            if (comps != null)
            {
                for (int i = 0; i < comps(__instance).Count; i++)
                {
                    comps(__instance)[i].PostDestroy(mode, map);
                }
            }
        }

        public static FieldRef<Thing, sbyte> mapIndexOrState = FieldRefAccess<Thing, sbyte>("mapIndexOrState");
        public static void ThingDestroy(Thing __instance, DestroyMode mode = DestroyMode.Vanish)
        {
            if (!Thing.allowDestroyNonDestroyable && !__instance.def.destroyable)
            {
                Log.Error("Tried to destroy non-destroyable thing " + __instance);
                return;
            }

            if (__instance.Destroyed)
            {
                Log.Error("Tried to destroy already-destroyed thing " + __instance);
                return;
            }

            bool spawned = __instance.Spawned;
            Map map = __instance.Map;
            if (__instance.Spawned)
            {
                __instance.DeSpawn(mode);
            }

            mapIndexOrState(__instance) = -2;
            if (__instance.def.DiscardOnDestroyed)
            {
                __instance.Discard();
            }

            CompExplosive compExplosive = __instance.TryGetComp<CompExplosive>();
            if (spawned)
            {
                List<Thing> list = (compExplosive != null) ? new List<Thing>() : null;
                GenLeaving.DoLeavingsFor(__instance, map, mode, list);
                compExplosive?.AddThingsIgnoredByExplosion(list);
            }

            if (__instance.holdingOwner != null)
            {
                __instance.holdingOwner.Notify_ContainedItemDestroyed(__instance);
            }

            RemoveAllReservationsAndDesignationsOnThis(__instance);
            if (!(__instance is Pawn))
            {
                __instance.stackCount = 0;
            }

            if (mode != DestroyMode.QuestLogic)
            {
                QuestUtility.SendQuestTargetSignals(__instance.questTags, "Destroyed", __instance.Named("SUBJECT"));
            }

            if (mode == DestroyMode.KillFinalize)
            {
                QuestUtility.SendQuestTargetSignals(__instance.questTags, "Killed", __instance.Named("SUBJECT"));
            }
        }
        private static void RemoveAllReservationsAndDesignationsOnThis(Thing __instance)
        {
            if (__instance.def.category == ThingCategory.Mote)
            {
                return;
            }

            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                maps[i].reservationManager.ReleaseAllForTarget(__instance);
                maps[i].physicalInteractionReservationManager.ReleaseAllForTarget(__instance);
                IAttackTarget attackTarget = __instance as IAttackTarget;
                if (attackTarget != null)
                {
                    maps[i].attackTargetReservationManager.ReleaseAllForTarget(attackTarget);
                }

                maps[i].designationManager.RemoveAllDesignationsOn(__instance);
            }
        }

    }
}
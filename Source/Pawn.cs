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
            ThingWithComps twc = __instance;
            twc.Destroy(mode);
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
                } catch (ArgumentOutOfRangeException)
                {
                    break;
                }
                if(p.playerSettings != null && p.playerSettings.Master == __instance)
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


    }
}

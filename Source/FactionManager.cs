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

namespace RimThreaded
{

    public class FactionManager_Patch
    {
        public static AccessTools.FieldRef<FactionManager, List<Faction>> allFactions =
            AccessTools.FieldRefAccess<FactionManager, List<Faction>>("allFactions");
        public static AccessTools.FieldRef<FactionManager, List<Faction>> toRemove =
            AccessTools.FieldRefAccess<FactionManager, List<Faction>>("toRemove");
        public static AccessTools.FieldRef<FactionManager, Faction> ofPlayer =
            AccessTools.FieldRefAccess<FactionManager, Faction>("ofPlayer");
        public static AccessTools.FieldRef<FactionManager, Faction> ofInsects =
            AccessTools.FieldRefAccess<FactionManager, Faction>("ofInsects");
        public static AccessTools.FieldRef<FactionManager, Faction> ofAncients =
            AccessTools.FieldRefAccess<FactionManager, Faction>("ofAncients");
        public static AccessTools.FieldRef<FactionManager, Faction> ofMechanoids =
            AccessTools.FieldRefAccess<FactionManager, Faction>("ofMechanoids");
        public static AccessTools.FieldRef<FactionManager, Faction> ofAncientsHostile =
            AccessTools.FieldRefAccess<FactionManager, Faction>("ofAncientsHostile");
        public static AccessTools.FieldRef<FactionManager, Faction> empire =
            AccessTools.FieldRefAccess<FactionManager, Faction>("empire");
        public static bool FactionManagerTick(FactionManager __instance)
        {
            SettlementProximityGoodwillUtility.CheckSettlementProximityGoodwillChange();

            RimThreaded.allFactions = allFactions(__instance);
            RimThreaded.allFactionsTicks = allFactions(__instance).Count;

            for (int num = toRemove(__instance).Count - 1; num >= 0; num--)
            {
                Faction faction = toRemove(__instance)[num];
                toRemove(__instance).Remove(faction);
                Remove2(__instance, faction);
                Find.QuestManager.Notify_FactionRemoved(faction);
            }
            return false;
        }

        private static void Remove2(FactionManager __instance, Faction faction)
        {
            if (!faction.temporary)
            {
                Log.Error("Attempting to remove " + faction.Name + " which is not a temporary faction, only temporary factions can be removed");
            }
            else
            {
                if (!allFactions(__instance).Contains(faction))
                {
                    return;
                }

                List<Pawn> allMapsWorldAndTemporary_AliveOrDead = PawnsFinder.AllMapsWorldAndTemporary_AliveOrDead;
                for (int i = 0; i < allMapsWorldAndTemporary_AliveOrDead.Count; i++)
                {
                    if (allMapsWorldAndTemporary_AliveOrDead[i].Faction == faction)
                    {
                        allMapsWorldAndTemporary_AliveOrDead[i].SetFaction(null);
                    }
                }

                for (int j = 0; j < Find.Maps.Count; j++)
                {
                    Find.Maps[j].pawnDestinationReservationManager.Notify_FactionRemoved(faction);
                }

                Find.LetterStack.Notify_FactionRemoved(faction);
                faction.RemoveAllRelations();
                allFactions(__instance).Remove(faction);
                RecacheFactions2(__instance);
            }
        }
        private static void RecacheFactions2(FactionManager __instance)
        {
            ofPlayer(__instance) = null;
            for (int i = 0; i < allFactions(__instance).Count; i++)
            {
                if (allFactions(__instance)[i].IsPlayer)
                {
                    ofPlayer(__instance) = allFactions(__instance)[i];
                    break;
                }
            }

            ofMechanoids(__instance) = __instance.FirstFactionOfDef(FactionDefOf.Mechanoid);
            ofInsects(__instance) = __instance.FirstFactionOfDef(FactionDefOf.Insect);
            ofAncients(__instance) = __instance.FirstFactionOfDef(FactionDefOf.Ancients);
            ofAncientsHostile(__instance) = __instance.FirstFactionOfDef(FactionDefOf.AncientsHostile);
            empire(__instance) = __instance.FirstFactionOfDef(FactionDefOf.Empire);
        }




    }
}

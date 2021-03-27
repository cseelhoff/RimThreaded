using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using RimWorld.Planet;
using static HarmonyLib.AccessTools;
using System.Reflection;

namespace RimThreaded
{

    public class FactionManager_Patch
    {
        public static FieldRef<FactionManager, List<Faction>> allFactions = FieldRefAccess<FactionManager, List<Faction>>("allFactions");
        public static FieldRef<FactionManager, List<Faction>> toRemove = FieldRefAccess<FactionManager, List<Faction>>("toRemove");
        public static FieldRef<FactionManager, Faction> ofPlayer = FieldRefAccess<FactionManager, Faction>("ofPlayer");
        public static FieldRef<FactionManager, Faction> ofInsects = FieldRefAccess<FactionManager, Faction>("ofInsects");
        public static FieldRef<FactionManager, Faction> ofAncients = FieldRefAccess<FactionManager, Faction>("ofAncients");
        public static FieldRef<FactionManager, Faction> ofMechanoids = FieldRefAccess<FactionManager, Faction>("ofMechanoids");
        public static FieldRef<FactionManager, Faction> ofAncientsHostile = FieldRefAccess<FactionManager, Faction>("ofAncientsHostile");
        public static FieldRef<FactionManager, Faction> empire = FieldRefAccess<FactionManager, Faction>("empire");

        private static readonly MethodInfo methodRemove =
            Method(typeof(FactionManager), "Remove", new Type[] { typeof(Faction) });
        private static readonly Action<FactionManager, Faction> actionRemove =
            (Action<FactionManager, Faction>)Delegate.CreateDelegate(typeof(Action<FactionManager, Faction>), methodRemove);

        public static bool FactionManagerTick(FactionManager __instance)
        {
            SettlementProximityGoodwillUtility.CheckSettlementProximityGoodwillChange();

            RimThreaded.allFactions = allFactions(__instance);
            RimThreaded.allFactionsTicks = allFactions(__instance).Count;

            for (int num = toRemove(__instance).Count - 1; num >= 0; num--)
            {
                Faction faction = toRemove(__instance)[num];
                toRemove(__instance).Remove(faction);
                actionRemove(__instance, faction);
                Find.QuestManager.Notify_FactionRemoved(faction);
            }
            return false;
        }

        internal static void RunDestructivePatches()
        {
            Type original = typeof(FactionManager);
            Type patched = typeof(FactionManager_Patch);
            RimThreadedHarmony.Prefix(original, patched, "FactionManagerTick");
        }
    }
}

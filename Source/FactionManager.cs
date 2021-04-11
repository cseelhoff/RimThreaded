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

        internal static void RunDestructivePatches()
        {
            Type original = typeof(FactionManager);
            Type patched = typeof(FactionManager_Patch);
            RimThreadedHarmony.Prefix(original, patched, "FactionManagerTick");
        }

        private static readonly MethodInfo methodRemove =
            Method(typeof(FactionManager), "Remove", new Type[] { typeof(Faction) });
        private static readonly Action<FactionManager, Faction> actionRemove =
            (Action<FactionManager, Faction>)Delegate.CreateDelegate(typeof(Action<FactionManager, Faction>), methodRemove);

        public static bool FactionManagerTick(FactionManager __instance)
        {
            SettlementProximityGoodwillUtility.CheckSettlementProximityGoodwillChange();

            RimThreaded.allFactions = allFactions(__instance);
            RimThreaded.allFactionsTicks = allFactions(__instance).Count;
            lock (__instance)
            {
                List<Faction> newList = toRemove(__instance);
                for (int num = newList.Count - 1; num >= 0; num--)
                {
                    Faction faction = newList[num];
                    newList.Remove(faction);
                    toRemove(__instance) = newList;
                    actionRemove(__instance, faction);
                    Find.QuestManager.Notify_FactionRemoved(faction);
                }
            }
            return false;
        }

    }
}

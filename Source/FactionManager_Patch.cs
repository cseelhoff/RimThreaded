using System;
using System.Collections.Generic;
using System.Threading;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RimThreaded
{

    public class FactionManager_Patch
    {
        internal static void RunDestructivePatches()
        {
            Type original = typeof(FactionManager);
            Type patched = typeof(FactionManager_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(FactionManagerTick));
        }
        
        public static bool FactionManagerTick(FactionManager __instance)
        {
            SettlementProximityGoodwillUtility.CheckSettlementProximityGoodwillChange();

            lock (__instance)
            {
                List<Faction> newList = __instance.toRemove;
                for (int num = newList.Count - 1; num >= 0; num--)
                {
                    Faction faction = newList[num];
                    newList.Remove(faction);
                    __instance.toRemove = newList;
                    __instance.Remove(faction);
#if RW12
                    Find.QuestManager.Notify_FactionRemoved(faction);
#endif
                }
            }
            allFactionsTickList = __instance.allFactions;
            allFactionsTicks = allFactionsTickList.Count;
            return false;
        }
        public static List<Faction> allFactionsTickList;
        public static int allFactionsTicks;

        public static void FactionsPrepare()
        {
            try
            {
                World world = Find.World;
                world.factionManager.FactionManagerTick();
            }
            catch (Exception ex3)
            {
                Log.Error(ex3.ToString());
            }
        }

        public static void FactionsListTick()
        {
            while (true)
            {
                int index = Interlocked.Decrement(ref allFactionsTicks);
                if (index < 0) return;
                Faction faction = allFactionsTickList[index];
                try
                {
                    faction.FactionTick();
                }
                catch (Exception ex)
                {
                    Log.Error("Exception ticking faction: " + faction.ToStringSafe() + ": " + ex);
                }
            }
        }
    }
}

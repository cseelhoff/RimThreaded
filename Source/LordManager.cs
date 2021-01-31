using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI.Group;

namespace RimThreaded
{
    class LordManager_Patch
    {
        public static bool LordOf(LordManager __instance, ref Lord __result, Pawn p)
        {
            Lord lordResult = null;
            if (p != null)
            {
                if (!Lord_Patch.pawnsLord.TryGetValue(p, out lordResult))
                {
                    for (int i = 0; i < __instance.lords.Count; i++)
                    {
                        Lord lord = __instance.lords[i];
                        for (int j = 0; j < lord.ownedPawns.Count; j++)
                        {
                            if (lord.ownedPawns[j] == p)
                            {
                                if(Lord_Patch.pawnsLord == null)
                                {
                                    Log.Error("Lord_Patch.pawnsLord is null");
                                }
                                lock (Lord_Patch.pawnsLord)
                                {
                                    Lord_Patch.pawnsLord.SetOrAdd(p, lord);
                                }
                                __result = lord;
                                return false;
                            }
                        }
                    }
                    try
                    {
                        lock (Lord_Patch.pawnsLord)
                        {
                            Lord_Patch.pawnsLord.SetOrAdd(p, null);
                        }
                    } catch (NullReferenceException)
                    {

                    }
                    
                }
            }
            __result = lordResult;
            return false;
        }
        public static bool RemoveLord(LordManager __instance, Lord oldLord)
        {
            for (int j = 0; j < oldLord.ownedPawns.Count; j++)
            {
                lock (Lord_Patch.pawnsLord)
                {
                    Lord_Patch.pawnsLord.SetOrAdd(oldLord.ownedPawns[j], null);
                }
            }
            __instance.lords.Remove(oldLord);
            Find.SignalManager.DeregisterReceiver(oldLord);
            oldLord.Cleanup();
            return false;
        }
    }
}
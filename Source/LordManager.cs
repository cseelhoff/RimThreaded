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
            if (!Lord_Patch.pawnsLord.TryGetValue(p, out lordResult))
            {
                for (int i = 0; i < __instance.lords.Count; i++)
                {
                    Lord lord = __instance.lords[i];
                    for (int j = 0; j < lord.ownedPawns.Count; j++)
                    {
                        if (lord.ownedPawns[j] == p)
                        {
                            Lord_Patch.pawnsLord[p] = lord;
                            __result = lord;
                            return false;
                        }
                    }
                }
                Lord_Patch.pawnsLord[p] = null;
            }
            __result = lordResult;
            return false;
        }
    }
}
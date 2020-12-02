using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace RimThreaded
{

    public class QuestUtility_Patch
    {

        public static bool GetExtraFaction(ref Faction __result, Pawn p, ExtraFactionType extraFactionType, Quest forQuest = null)
        {
            //tmpExtraFactions.Clear();
            List<ExtraFaction> tmpExtraFactions = new List<ExtraFaction>();
            QuestUtility.GetExtraFactionsFromQuestParts(p, tmpExtraFactions, forQuest);
            for (int i = 0; i < tmpExtraFactions.Count; i++)
            {
                if (tmpExtraFactions[i].factionType == extraFactionType)
                {
                    Faction faction = tmpExtraFactions[i].faction;
                    tmpExtraFactions.Clear();
                    __result = faction;
                    return false;
                }
            }

            //tmpExtraFactions.Clear();
            __result = null;
            return false;
        }
    }
}
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{

    public class CompCauseGameCondition_Patch
    {
        public static FieldRef<CompCauseGameCondition, Dictionary<Map, GameCondition>> causedConditions = 
            FieldRefAccess<CompCauseGameCondition, Dictionary<Map, GameCondition>>("causedConditions");

        public static bool CompTick(CompCauseGameCondition __instance)
		{
            if (__instance.Active)
            {
                foreach (Map map in Find.Maps)
                {
                    if (__instance.InAoE(map.Tile))
                    {
                        EnforceConditionOn2(__instance, map);
                    }
                }
            }

            List<Map> tmpDeadConditionMaps = new List<Map>();
            foreach (KeyValuePair<Map, GameCondition> causedCondition in causedConditions(__instance))
            {
                if (causedCondition.Value.Expired || !causedCondition.Key.GameConditionManager.ConditionIsActive(causedCondition.Value.def))
                {
                    tmpDeadConditionMaps.Add(causedCondition.Key);
                }
            }

            foreach (Map tmpDeadConditionMap in tmpDeadConditionMaps)
            {
                causedConditions(__instance).Remove(tmpDeadConditionMap);
            }
            return false;
        }
        public static GameCondition EnforceConditionOn2(CompCauseGameCondition __instance, Map map)
        {
            GameCondition gameCondition = GetConditionInstance2(__instance, map);
            if (gameCondition == null)
            {
                gameCondition = CreateConditionOn2(__instance, map);
            }
            else
            {
                gameCondition.TicksLeft = gameCondition.TransitionTicks;
            }

            return gameCondition;
        }
        public static GameCondition GetConditionInstance2(CompCauseGameCondition __instance, Map map)
        {
            if (!causedConditions(__instance).TryGetValue(map, out GameCondition value) && __instance.Props.preventConditionStacking)
            {
                value = map.GameConditionManager.GetActiveCondition(__instance.Props.conditionDef);
                if (value != null)
                {
                    causedConditions(__instance).Add(map, value);
                    SetupCondition2(value, map);
                }
            }

            return value;
        }
        public static GameCondition CreateConditionOn2(CompCauseGameCondition __instance, Map map)
        {
            GameCondition gameCondition = GameConditionMaker.MakeCondition(__instance.ConditionDef);
            gameCondition.Duration = gameCondition.TransitionTicks;
            gameCondition.conditionCauser = __instance.parent;
            map.gameConditionManager.RegisterCondition(gameCondition);
            causedConditions(__instance).Add(map, gameCondition);
            SetupCondition2(gameCondition, map);
            return gameCondition;
        }
        public static void SetupCondition2(GameCondition condition, Map map)
        {
            condition.suppressEndMessage = true;
        }




    }
}

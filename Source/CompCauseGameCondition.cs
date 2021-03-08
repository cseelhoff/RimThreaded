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
using System.Reflection;

namespace RimThreaded
{

    public class CompCauseGameCondition_Patch
    {
        [ThreadStatic]
        private static List<Map> tmpDeadConditionMaps;

        public static FieldRef<CompCauseGameCondition, Dictionary<Map, GameCondition>> causedConditions = 
            FieldRefAccess<CompCauseGameCondition, Dictionary<Map, GameCondition>>("causedConditions");

        private static readonly MethodInfo methodEnforceConditionOn =
            Method(typeof(CompCauseGameCondition), "EnforceConditionOn", new Type[] { typeof(Action) });
        private static readonly Action<CompCauseGameCondition, Map> actionEnforceConditionOn =
            (Action<CompCauseGameCondition, Map>)Delegate.CreateDelegate(typeof(Action<CompCauseGameCondition, Map>), methodEnforceConditionOn);

        public static bool CompTick(CompCauseGameCondition __instance)
		{
            if (__instance.Active)
            {
                foreach (Map map in Find.Maps)
                {
                    if (__instance.InAoE(map.Tile))
                    {
                        actionEnforceConditionOn(__instance, map);
                    }
                }
            }
            if (tmpDeadConditionMaps == null)
            {
                tmpDeadConditionMaps = new List<Map>();
            } else
            {
                tmpDeadConditionMaps.Clear();
            }

            foreach (KeyValuePair<Map, GameCondition> causedCondition in causedConditions(__instance))
            {
                if (causedCondition.Value.Expired || !causedCondition.Key.GameConditionManager.ConditionIsActive(causedCondition.Value.def))
                {
                    tmpDeadConditionMaps.Add(causedCondition.Key);
                }
            }

            foreach (Map tmpDeadConditionMap in tmpDeadConditionMaps)
            {
                if (causedConditions(__instance).ContainsKey(tmpDeadConditionMap))
                {
                    lock (__instance)
                    {
                        causedConditions(__instance).Remove(tmpDeadConditionMap); //TODO is this thread safe?
                    }
                }
            }
            return false;
        }
    




    }
}

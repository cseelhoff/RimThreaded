using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using static HarmonyLib.AccessTools;
using System.Reflection;

namespace RimThreaded
{

    public class CompCauseGameCondition_Patch
    {
        [ThreadStatic] public static List<Map> tmpDeadConditionMaps;

        public static FieldRef<CompCauseGameCondition, Dictionary<Map, GameCondition>> causedConditionsFieldRef = 
            FieldRefAccess<CompCauseGameCondition, Dictionary<Map, GameCondition>>("causedConditions");

        private static readonly MethodInfo methodEnforceConditionOn =
            Method(typeof(CompCauseGameCondition), "EnforceConditionOn", new Type[] { typeof(Map) });
        private static readonly Func<CompCauseGameCondition, Map, GameCondition> funcEnforceConditionOn =
            (Func<CompCauseGameCondition, Map, GameCondition>)Delegate.CreateDelegate(typeof(Func<CompCauseGameCondition, Map, GameCondition>), methodEnforceConditionOn);

        public static void InitializeThreadStatics()
        {
            tmpDeadConditionMaps = new List<Map>();
        }

        public static bool GetConditionInstance(CompCauseGameCondition __instance, ref GameCondition __result, Map map)
        {
            if (!causedConditionsFieldRef(__instance).TryGetValue(map, out GameCondition value) && __instance.Props.preventConditionStacking)
            {
                value = map.GameConditionManager.GetActiveCondition(__instance.Props.conditionDef);
                if (value != null)
                {
                    lock (__instance)
                    {
                        causedConditionsFieldRef(__instance).Add(map, value);
                    }
                    value.suppressEndMessage = true;
                }
            }

            __result = value;
            return false;
        }
        public static bool CreateConditionOn(CompCauseGameCondition __instance, ref GameCondition __result, Map map)
        {
            GameCondition gameCondition = GameConditionMaker.MakeCondition(__instance.ConditionDef);
            gameCondition.Duration = gameCondition.TransitionTicks;
            gameCondition.conditionCauser = __instance.parent;
            map.gameConditionManager.RegisterCondition(gameCondition);
            lock (__instance)
            {
                causedConditionsFieldRef(__instance).Add(map, gameCondition);
            }
            gameCondition.suppressEndMessage = true;
            __result = gameCondition;
            return false;
        }

        public static bool CompTick(CompCauseGameCondition __instance)
		{
            if (__instance.Active)
            {
                foreach (Map map in Find.Maps)
                {
                    if (__instance.InAoE(map.Tile))
                    {
                        funcEnforceConditionOn(__instance, map);
                    }
                }
            }
            tmpDeadConditionMaps.Clear();

            foreach (KeyValuePair<Map, GameCondition> causedCondition in causedConditionsFieldRef(__instance))
            {
                if (causedCondition.Value.Expired || !causedCondition.Key.GameConditionManager.ConditionIsActive(causedCondition.Value.def))
                {
                    tmpDeadConditionMaps.Add(causedCondition.Key);
                }
            }

            foreach (Map tmpDeadConditionMap in tmpDeadConditionMaps)
            {
                if (causedConditionsFieldRef(__instance).ContainsKey(tmpDeadConditionMap))
                {
                    lock (__instance)
                    {
                        if (causedConditionsFieldRef(__instance).ContainsKey(tmpDeadConditionMap))
                        {
                            Dictionary<Map, GameCondition> newCausedConditions = new Dictionary<Map, GameCondition>(causedConditionsFieldRef(__instance));
                            newCausedConditions.Remove(tmpDeadConditionMap);
                            causedConditionsFieldRef(__instance) = newCausedConditions;
                        }
                    }
                }
            }
            return false;
        }

        internal static void RunDestructivePatches()
        {
            Type original = typeof(CompCauseGameCondition);
            Type patched = typeof(CompCauseGameCondition_Patch);
            RimThreadedHarmony.Prefix(original, patched, "GetConditionInstance");
            RimThreadedHarmony.Prefix(original, patched, "CreateConditionOn");
            RimThreadedHarmony.Prefix(original, patched, "CompTick");
        }
    }
}

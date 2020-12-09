using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{
    class Building_Trap_Patch
    {
        public static FieldRef<Building_Trap, List<Pawn>> touchingPawns = 
            FieldRefAccess<Building_Trap, List<Pawn>>("touchingPawns");
        public static bool Tick(Building_Trap __instance)
        {
            Building building = __instance;
            if (building.Spawned)
            {
                List<Thing> thingList = building.Position.GetThingList(building.Map);
                for (int i = 0; i < thingList.Count; i++)
                {
                    Pawn pawn;
                    try
                    {
                        pawn = thingList[i] as Pawn;
                    } catch (ArgumentOutOfRangeException) { break; }
                    if (pawn != null && !touchingPawns(__instance).Contains(pawn))
                    {
                        touchingPawns(__instance).Add(pawn);
                        CheckSpring2(__instance, pawn);
                    }
                }

                for (int j = 0; j < touchingPawns(__instance).Count; j++)
                {
                    Pawn pawn2;
                    try
                    {
                        pawn2 = touchingPawns(__instance)[j];
                    }
                    catch (ArgumentOutOfRangeException) { break; }
                    if (!pawn2.Spawned || pawn2.Position != building.Position)
                    {
                        touchingPawns(__instance).Remove(pawn2);
                    }
                }
            }

            building.Tick();
            return false;
        }
        private static void CheckSpring2(Building_Trap __instance, Pawn p)
        {
            if (Rand.Chance(SpringChance2(__instance, p)))
            {
                Building building = __instance;
                Map map = building.Map;
                __instance.Spring(p);
                if (p.Faction == Faction.OfPlayer || p.HostFaction == Faction.OfPlayer)
                {
                    Find.LetterStack.ReceiveLetter(
                        "LetterFriendlyTrapSprungLabel".Translate(p.LabelShort, p).CapitalizeFirst(),
                        "LetterFriendlyTrapSprung".Translate(p.LabelShort, p).CapitalizeFirst(), 
                        LetterDefOf.NegativeEvent, new TargetInfo(building.Position, map));
                }
            }
        }
        protected static float SpringChance2(Building_Trap __instance, Pawn p)
        {
            float num = 1f;
            if (__instance.KnowsOfTrap(p))
            {
                if (p.Faction != null)
                {
                    Building building = __instance;
                    num = ((p.Faction != building.Faction) ? 0f : 0.005f);
                }
                else if (p.RaceProps.Animal)
                {
                    num = 0.2f;
                    num *= __instance.def.building.trapPeacefulWildAnimalsSpringChanceFactor;
                }
                else
                {
                    num = 0.3f;
                }
            }

            num *= __instance.GetStatValue(StatDefOf.TrapSpringChance) * p.GetStatValue(StatDefOf.PawnTrapSpringChance);
            return Mathf.Clamp01(num);
        }



    }
}

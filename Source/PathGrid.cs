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

    public class PathGrid_Patch
	{
		public static AccessTools.FieldRef<PathGrid, Map> map =
			AccessTools.FieldRefAccess<PathGrid, Map>("map");
        private static bool IsPathCostIgnoreRepeater(ThingDef def)
        {
            if (def.pathCost >= 25)
            {
                return def.pathCostIgnoreRepeat;
            }

            return false;
        }

        private static bool ContainsPathCostIgnoreRepeater2(PathGrid __instance, IntVec3 c)
        {
            List<Thing> list = map(__instance).thingGrid.ThingsListAt(c);
            for (int i = 0; i < list.Count; i++)
            {
                Thing thing;
                try
                {
                    thing = list[i];
                } catch (ArgumentOutOfRangeException) { break; }
                if (thing!=null && IsPathCostIgnoreRepeater(thing.def))
                {
                    return true;
                }
            }

            return false;
        }


        public static bool CalculatedCostAt(PathGrid __instance, ref int __result, IntVec3 c, bool perceivedStatic, IntVec3 prevCell)
        {
            int num = 0;
            bool flag = false;
            TerrainDef terrainDef = map(__instance).terrainGrid.TerrainAt(c);
            if (terrainDef == null || terrainDef.passability == Traversability.Impassable)
            {
                __result = 10000;
                return false;
            }

            num = terrainDef.pathCost;
            List<Thing> list = map(__instance).thingGrid.ThingsListAt(c);
            for (int i = 0; i < list.Count; i++)
            {
                Thing thing = list[i];
                if (thing != null)
                {
                    if (thing.def.passability == Traversability.Impassable)
                    {
                        __result = 10000;
                        return false;
                    }

                    if (!IsPathCostIgnoreRepeater(thing.def) || !prevCell.IsValid || !ContainsPathCostIgnoreRepeater2(__instance, prevCell))
                    {
                        int pathCost = thing.def.pathCost;
                        if (pathCost > num)
                        {
                            num = pathCost;
                        }
                    }

                    if (thing is Building_Door && prevCell.IsValid)
                    {
                        Building edifice = prevCell.GetEdifice(map(__instance));
                        if (edifice != null && edifice is Building_Door)
                        {
                            flag = true;
                        }
                    }
                }
            }

            int num2 = SnowUtility.MovementTicksAddOn(map(__instance).snowGrid.GetCategory(c));
            if (num2 > num)
            {
                num = num2;
            }

            if (flag)
            {
                num += 45;
            }

            if (perceivedStatic)
            {
                for (int j = 0; j < 9; j++)
                {
                    IntVec3 b = GenAdj.AdjacentCellsAndInside[j];
                    IntVec3 c2 = c + b;
                    if (!c2.InBounds(map(__instance)))
                    {
                        continue;
                    }

                    Fire fire = null;
                    list = map(__instance).thingGrid.ThingsListAtFast(c2);
                    for (int k = 0; k < list.Count; k++)
                    {
                        fire = (list[k] as Fire);
                        if (fire != null)
                        {
                            break;
                        }
                    }

                    if (fire != null && fire.parent == null)
                    {
                        num = ((b.x != 0 || b.z != 0) ? (num + 150) : (num + 1000));
                    }
                }
            }

            __result = num;
            return false;
        }



    }
}

using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{
    class World_Patch
    {
        [ThreadStatic]
        static List<ThingDef> tmpNaturalRockDefs;

        [ThreadStatic]
        static List<Rot4> tmpOceanDirs;

        [ThreadStatic]
        static List<int> tmpNeighbors;

        public static FieldRef<World, List<ThingDef>> allNaturalRockDefsFieldRef = 
            FieldRefAccess<World, List<ThingDef>>("allNaturalRockDefs");

        public static bool NaturalRockTypesIn(World __instance, ref IEnumerable<ThingDef> __result, int tile)
        {
            if (tmpNaturalRockDefs == null)
            {
                tmpNaturalRockDefs = new List<ThingDef>();
            } else
            {
                tmpNaturalRockDefs.Clear();
            }
            Rand.PushState();
            Rand.Seed = tile;
            if (allNaturalRockDefsFieldRef(__instance) == null)
            {
                allNaturalRockDefsFieldRef(__instance) = DefDatabase<ThingDef>.AllDefs.Where((ThingDef d) => d.IsNonResourceNaturalRock).ToList();
            }

            int num = Rand.RangeInclusive(2, 3);
            if (num > allNaturalRockDefsFieldRef(__instance).Count)
            {
                num = allNaturalRockDefsFieldRef(__instance).Count;
            }

            tmpNaturalRockDefs.Clear();
            tmpNaturalRockDefs.AddRange(allNaturalRockDefsFieldRef(__instance));
            List<ThingDef> list = new List<ThingDef>();
            for (int i = 0; i < num; i++)
            {
                ThingDef item = tmpNaturalRockDefs.RandomElement();
                tmpNaturalRockDefs.Remove(item);
                list.Add(item);
            }

            Rand.PopState();
            __result = list;
            return false;
        }

        public static bool CoastDirectionAt(World __instance, ref Rot4 __result, int tileID)
        {
            if (!__instance.grid[tileID].biome.canBuildBase)
            {
                __result = Rot4.Invalid;
                return false;
            }
            if (tmpOceanDirs == null)
            {
                tmpOceanDirs = new List<Rot4>();
            }
            else
            {
                tmpOceanDirs.Clear();
            }
            if (tmpNeighbors == null)
            {
                tmpNeighbors = new List<int>();
            }
            __instance.grid.GetTileNeighbors(tileID, tmpNeighbors);
            int i = 0;
            for (int count = tmpNeighbors.Count; i < count; i++)
            {
                if (__instance.grid[tmpNeighbors[i]].biome == BiomeDefOf.Ocean)
                {
                    Rot4 rotFromTo = __instance.grid.GetRotFromTo(tileID, tmpNeighbors[i]);
                    if (!tmpOceanDirs.Contains(rotFromTo))
                    {
                        tmpOceanDirs.Add(rotFromTo);
                    }
                }
            }

            if (tmpOceanDirs.Count == 0)
            {
                __result = Rot4.Invalid;
                return false;
            }

            Rand.PushState();
            Rand.Seed = tileID;
            int index = Rand.Range(0, tmpOceanDirs.Count);
            Rand.PopState();
            __result = tmpOceanDirs[index];
            return false;
        }


    }
}

using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{
    class World_Patch
    {

        public static FieldRef<World, List<ThingDef>> allNaturalRockDefs = FieldRefAccess<World, List<ThingDef>>("allNaturalRockDefs");

        public static bool NaturalRockTypesIn(World __instance, ref IEnumerable<ThingDef> __result, int tile)
        {
            List<ThingDef> tmpNaturalRockDefs = new List<ThingDef>();
            Rand.PushState();
            Rand.Seed = tile;
            if (allNaturalRockDefs(__instance) == null)
            {
                allNaturalRockDefs(__instance) = DefDatabase<ThingDef>.AllDefs.Where((ThingDef d) => d.IsNonResourceNaturalRock).ToList();
            }

            int num = Rand.RangeInclusive(2, 3);
            if (num > allNaturalRockDefs(__instance).Count)
            {
                num = allNaturalRockDefs(__instance).Count;
            }

            tmpNaturalRockDefs.Clear();
            tmpNaturalRockDefs.AddRange(allNaturalRockDefs(__instance));
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
    }
}

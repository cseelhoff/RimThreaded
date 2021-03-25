using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using Verse;

namespace RimThreaded
{
    class World_Patch
    {
        [ThreadStatic] public static List<ThingDef> tmpNaturalRockDefs;
        [ThreadStatic] public static List<Rot4> tmpOceanDirs;
        [ThreadStatic] public static List<int> tmpNeighbors;

        public static void InitializeThreadStatics()
        {
            tmpNaturalRockDefs = new List<ThingDef>();
            tmpOceanDirs = new List<Rot4>();
            tmpNeighbors = new List<int>();
        }

        public static void RunNonDestructivePatches()
        {
            Type original = typeof(World);
            Type patched = typeof(World_Patch);
            RimThreadedHarmony.threadStaticPatches.Add(original, patched);
            RimThreadedHarmony.TranspileThreadStatics(original, "NaturalRockTypesIn");
            RimThreadedHarmony.TranspileThreadStatics(original, "CoastDirectionAt");
        }

    }
}

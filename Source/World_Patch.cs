using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimThreaded
{
    class World_Patch
    {
        [ThreadStatic] public static List<ThingDef> tmpNaturalRockDefs = new List<ThingDef>();
        [ThreadStatic] public static List<int> tmpNeighbors = new List<int>();
        [ThreadStatic] public static List<Rot4> tmpOceanDirs = new List<Rot4>();

        internal static void InitializeThreadStatics()
        {
            tmpNaturalRockDefs = new List<ThingDef>();
            tmpNeighbors = new List<int>();
            tmpOceanDirs = new List<Rot4>();
        }

    }
}

using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using Verse.AI;

namespace RimThreaded
{
    
    public class RegionTraverser2
    {
        public int NumWorkers = 8;
        public readonly RegionEntryPredicate PassAll = ((from, to) => true);
        public Dictionary<Region, uint[]> regionClosedIndex = new Dictionary<Region, uint[]>();


    }

}

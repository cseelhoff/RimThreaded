using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RimThreaded
{

    public class InfestationCellFinder_Patch
    {
        public struct LocationCandidate
        {
            public IntVec3 cell;

            public float score;

            public LocationCandidate(IntVec3 cell, float score)
            {
                this.cell = cell;
                this.score = score;
            }
        }
        [ThreadStatic] public static Dictionary<Region, float> regionsDistanceToUnroofed;
        [ThreadStatic] public static List<IntVec3> tmpColonyBuildingsLocs;
        [ThreadStatic] public static List<KeyValuePair<IntVec3, float>> tmpDistanceResult;
        [ThreadStatic] public static ByteGrid distToColonyBuilding;
        [ThreadStatic] public static ByteGrid closedAreaSize;
        [ThreadStatic] public static List<Pair<IntVec3, float>> tmpCachedInfestationChanceCellColors;
        [ThreadStatic] public static HashSet<Region> tempUnroofedRegions;
        [ThreadStatic] public static List<LocationCandidate> locationCandidates;

        public static void InitializeThreadStatics()
        {
            tmpColonyBuildingsLocs = new List<IntVec3>();
            tmpDistanceResult = new List<KeyValuePair<IntVec3, float>>();
            tmpCachedInfestationChanceCellColors = new List<Pair<IntVec3, float>>();
            tempUnroofedRegions = new HashSet<Region>();
            locationCandidates = new List<LocationCandidate>();
            regionsDistanceToUnroofed = new Dictionary<Region, float>();
        }

        public static void RunNonDestructivePatches()
        {
            Type original = typeof(InfestationCellFinder);
            Type patched = typeof(InfestationCellFinder_Patch);
            RimThreadedHarmony.threadStaticPatches.Add(original, patched);
            RimThreadedHarmony.TranspileThreadStatics(original, "GetScoreAt");
            RimThreadedHarmony.TranspileThreadStatics(original, "DebugDraw");
            RimThreadedHarmony.TranspileThreadStatics(original, "CalculateLocationCandidates");
            RimThreadedHarmony.TranspileThreadStatics(original, "CalculateTraversalDistancesToUnroofed");
            RimThreadedHarmony.TranspileThreadStatics(original, "CalculateClosedAreaSizeGrid");
            RimThreadedHarmony.TranspileThreadStatics(original, "CalculateDistanceToColonyBuildingGrid");
        }

    }
}

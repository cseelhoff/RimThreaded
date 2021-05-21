using System;
using System.Collections.Generic;
using RimWorld;
using Verse;

namespace RimThreaded
{

    public class BeautyUtility_Patch
    {
        [ThreadStatic] public static List<Thing> tempCountedThings;
        [ThreadStatic] public static List<IntVec3> tmpBeautyRelevantCells;
        [ThreadStatic] public static List<Room> tmpVisibleRooms;
        [ThreadStatic] public static List<IntVec3> beautyRelevantCells;
        [ThreadStatic] public static List<Room> visibleRooms;

        public static void InitializeThreadStatics()
        {
            tempCountedThings = new List<Thing>();
            tmpBeautyRelevantCells = new List<IntVec3>();
            tmpVisibleRooms = new List<Room>();
            beautyRelevantCells = new List<IntVec3>();
            visibleRooms = new List<Room>();
        }

        internal static void RunNonDestructivePatches()
        {
            Type original = typeof(BeautyUtility);
            Type patched = typeof(BeautyUtility_Patch);
            RimThreadedHarmony.AddAllMatchingFields(original, patched);
            RimThreadedHarmony.TranspileFieldReplacements(original, "AverageBeautyPerceptible");
            RimThreadedHarmony.TranspileFieldReplacements(original, "FillBeautyRelevantCells");
        }

    }
}
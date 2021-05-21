using System;
using System.Collections.Generic;
using Verse;

namespace RimThreaded
{
    class RegionMaker_Patch
    {

        [ThreadStatic] public static HashSet<Thing> tmpProcessedThings;
        
        static readonly Type original = typeof(RegionMaker);
        static readonly Type patched = typeof(RegionMaker_Patch);
        public static void InitializeThreadStatics()
        {
            tmpProcessedThings = new HashSet<Thing>();
        }
        public static void RunDestructivePatches()
        {
            RimThreadedHarmony.Prefix(original, patched, "FloodFillAndAddCells");
            RimThreadedHarmony.Prefix(original, patched, "CreateLinks");
        }

        public static void RunNonDestructivePatches()
        {
            RimThreadedHarmony.AddAllMatchingFields(original, patched);
            RimThreadedHarmony.TranspileFieldReplacements(original, "RegisterThingsInRegionListers");
        }


        public static bool FloodFillAndAddCells(RegionMaker __instance, IntVec3 root)
        {
            Region newReg = __instance.newReg;
            Map map = __instance.map;
            __instance.newRegCells = new List<IntVec3>();
            if (newReg.type.IsOneCellRegion())
            {
                if (!RegionAndRoomUpdater_Patch.cellsWithNewRegions.Contains(root))
                {
                    RegionAndRoomUpdater_Patch.cellsWithNewRegions.Add(root);
                    __instance.AddCell(root);
                }

                return false;
            }

            map.floodFiller.FloodFill(root, x => newReg.extentsLimit.Contains(x) && x.GetExpectedRegionType(map) == newReg.type, delegate (IntVec3 x)
            {
                if (!RegionAndRoomUpdater_Patch.cellsWithNewRegions.Contains(x))
                {
                    RegionAndRoomUpdater_Patch.cellsWithNewRegions.Add(x);
                    __instance.AddCell(x);
                }
            });
            return false;
        }
        public static bool CreateLinks(RegionMaker __instance)
        {
            HashSet<IntVec3>[] linksProcessedAt = __instance.linksProcessedAt;
            List<IntVec3> newRegCells = __instance.newRegCells;
            for (int i = 0; i < linksProcessedAt.Length; i++)
            {
                linksProcessedAt[i] = new HashSet<IntVec3>();
            }

            for (int j = 0; j < newRegCells.Count; j++)
            {
                IntVec3 c = newRegCells[j];
                __instance.SweepInTwoDirectionsAndTryToCreateLink(Rot4.North, c);
                __instance.SweepInTwoDirectionsAndTryToCreateLink(Rot4.South, c);
                __instance.SweepInTwoDirectionsAndTryToCreateLink(Rot4.East, c);
                __instance.SweepInTwoDirectionsAndTryToCreateLink(Rot4.West, c);
            }
            return false;
        }
    }
}

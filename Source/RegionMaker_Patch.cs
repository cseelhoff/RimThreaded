using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{
    class RegionMaker_Patch
    {

        [ThreadStatic] public static HashSet<Thing> tmpProcessedThings;

        private static readonly FieldRef<RegionMaker, List<IntVec3>> newRegCellsFieldRef = FieldRefAccess<RegionMaker, List<IntVec3>>("newRegCells");
        private static readonly FieldRef<RegionMaker, Region> newRegFieldRef = FieldRefAccess<RegionMaker, Region>("newReg");
        private static readonly FieldRef<RegionMaker, Map> mapFieldRef = FieldRefAccess<RegionMaker, Map>("map");
        private static readonly FieldRef<RegionMaker, HashSet<IntVec3>[]> linksProcessedAtFieldRef = FieldRefAccess<RegionMaker, HashSet<IntVec3>[]>("linksProcessedAt");


        private static readonly MethodInfo methodAddCell =
            Method(typeof(RegionMaker), "AddCell", new Type[] { typeof(IntVec3) });
        private static readonly Action<RegionMaker, IntVec3> actionAddCell =
            (Action<RegionMaker, IntVec3>)Delegate.CreateDelegate(
                typeof(Action<RegionMaker, IntVec3>), methodAddCell);

        private static readonly MethodInfo methodSweepInTwoDirectionsAndTryToCreateLink =
            Method(typeof(RegionMaker), "SweepInTwoDirectionsAndTryToCreateLink", new Type[] { typeof(Rot4), typeof(IntVec3) });
        private static readonly Action<RegionMaker, Rot4, IntVec3> actionSweepInTwoDirectionsAndTryToCreateLink =
            (Action<RegionMaker, Rot4, IntVec3>)Delegate.CreateDelegate(
                typeof(Action<RegionMaker, Rot4, IntVec3>), methodSweepInTwoDirectionsAndTryToCreateLink);

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
            Region newReg = newRegFieldRef(__instance);
            Map map = mapFieldRef(__instance);
            newRegCellsFieldRef(__instance) = new List<IntVec3>();
            if (newReg.type.IsOneCellRegion())
            {
                if (!RegionAndRoomUpdater_Patch.cellsWithNewRegions.Contains(root))
                {
                    RegionAndRoomUpdater_Patch.cellsWithNewRegions.Add(root);
                    actionAddCell(__instance, root);
                }

                return false;
            }

            map.floodFiller.FloodFill(root, (IntVec3 x) => newReg.extentsLimit.Contains(x) && x.GetExpectedRegionType(map) == newReg.type, delegate (IntVec3 x)
            {
                if (!RegionAndRoomUpdater_Patch.cellsWithNewRegions.Contains(x))
                {
                    RegionAndRoomUpdater_Patch.cellsWithNewRegions.Add(x);
                    actionAddCell(__instance, x);
                }
            });
            return false;
        }
        public static bool CreateLinks(RegionMaker __instance)
        {
            HashSet<IntVec3>[] linksProcessedAt = linksProcessedAtFieldRef(__instance);
            List<IntVec3> newRegCells = newRegCellsFieldRef(__instance);
            for (int i = 0; i < linksProcessedAt.Length; i++)
            {
                linksProcessedAt[i] = new HashSet<IntVec3>();
            }

            for (int j = 0; j < newRegCells.Count; j++)
            {
                IntVec3 c = newRegCells[j];
                actionSweepInTwoDirectionsAndTryToCreateLink(__instance, Rot4.North, c);
                actionSweepInTwoDirectionsAndTryToCreateLink(__instance, Rot4.South, c);
                actionSweepInTwoDirectionsAndTryToCreateLink(__instance, Rot4.East, c);
                actionSweepInTwoDirectionsAndTryToCreateLink(__instance, Rot4.West, c);
            }
            return false;
        }
    }
}

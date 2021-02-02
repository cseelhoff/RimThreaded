using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using UnityEngine;
using System.Reflection;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{

    public class RCellFinder_Patch
    {
        [ThreadStatic]
        public static List<Region> regions = new List<Region>();
        [ThreadStatic]
        public static HashSet<Thing> tmpBuildings = new HashSet<Thing>();
        [ThreadStatic]
        public static List<Thing> tmpSpotThings = new List<Thing>();
        [ThreadStatic]
        public static List<IntVec3> tmpSpotsToAvoid = new List<IntVec3>();
        [ThreadStatic]
        public static List<IntVec3> tmpEdgeCells = new List<IntVec3>();

        private static readonly MethodInfo methodCanWanderToCell = Method(typeof(RCellFinder), "CanWanderToCell");
        private static readonly Func<IntVec3, Pawn, IntVec3, Func<Pawn, IntVec3, IntVec3, bool>, int, Danger, bool> funcCanWanderToCell =
            (Func< IntVec3, Pawn, IntVec3, Func<Pawn, IntVec3, IntVec3, bool>, int, Danger, bool>)Delegate.CreateDelegate(
                typeof(Func<IntVec3, Pawn, IntVec3, Func<Pawn, IntVec3, IntVec3, bool>, int, Danger, bool>), methodCanWanderToCell);

        public static bool RandomWanderDestFor(ref IntVec3 __result, Pawn pawn,
            IntVec3 root,
            float radius,
            Func<Pawn, IntVec3, IntVec3, bool> validator,
            Danger maxDanger)
        {
            if (radius > 12f)
            {
                Log.Warning("wanderRadius of " + radius + " is greater than Region.GridSize of " + 12 + " and will break.");
            }

            bool flag = false;
            if (root.GetRegion(pawn.Map) != null)
            {
                int maxRegions = Mathf.Max((int)radius / 3, 13);
                List<Region> regions = new List<Region>();
                CellFinder.AllRegionsNear(regions, root.GetRegion(pawn.Map), maxRegions, TraverseParms.For(pawn), (Region reg) => reg.extentsClose.ClosestDistSquaredTo(root) <= radius * radius);
                if (flag)
                {
                    pawn.Map.debugDrawer.FlashCell(root, 0.6f, "root");
                }
                if (regions.Count > 0)
                {
                    for (int tryIndex = 0; tryIndex < 35; ++tryIndex)
                    {
                        IntVec3 c = IntVec3.Invalid;
                        for (int index = 0; index < 5; ++index)
                        {
                            _ = regions.TryRandomElementByWeight((reg => reg.CellCount), out Region randomRegion);
                            if (randomRegion != null)
                            {
                                IntVec3 randomCell = randomRegion.RandomCell;
                                if (randomCell.DistanceToSquared(root) <= radius * radius)
                                {
                                    c = randomCell;
                                    break;
                                }
                            }
                        }
                        if (!c.IsValid)
                        {
                            if (flag)
                                pawn.Map.debugDrawer.FlashCell(c, 0.32f, "distance", 50);
                        }
                        else if (!funcCanWanderToCell(c, pawn, root, validator, tryIndex, maxDanger))
                        {
                            if (flag)
                                pawn.Map.debugDrawer.FlashCell(c, 0.6f, "validation", 50);
                        }
                        else
                        {
                            if (flag)
                                pawn.Map.debugDrawer.FlashCell(c, 0.9f, "go!", 50);
                            regions.Clear();
                            __result = c;
                            return false;
                        }
                    }
                }
                regions.Clear();
            }
            IntVec3 result;
            if (!CellFinder.TryFindRandomCellNear(root, pawn.Map, Mathf.FloorToInt(radius), c =>
                {
                    if (!c.InBounds(pawn.Map) || !pawn.CanReach(c, PathEndMode.OnCell, Danger.None, false, TraverseMode.ByPawn) || c.IsForbidden(pawn))
                        return false;
                    return validator == null || validator(pawn, c, root);
                }, out result, -1) && !CellFinder.TryFindRandomCellNear(root, pawn.Map, Mathf.FloorToInt(radius), c => c.InBounds(pawn.Map) && 
                pawn.CanReach(c, PathEndMode.OnCell, Danger.None, false, TraverseMode.ByPawn) && !c.IsForbidden(pawn), out result, -1) && 
                (!CellFinder.TryFindRandomCellNear(root, pawn.Map, Mathf.FloorToInt(radius), c => c.InBounds(pawn.Map) && 
                pawn.CanReach(c, PathEndMode.OnCell, Danger.Deadly, false, TraverseMode.ByPawn), out result, -1) && 
                !CellFinder.TryFindRandomCellNear(root, pawn.Map, 20, c => c.InBounds(pawn.Map) && 
                pawn.CanReach(c, PathEndMode.OnCell, Danger.None, false, TraverseMode.ByPawn) && !c.IsForbidden(pawn), out result, -1)) && 
                (!CellFinder.TryFindRandomCellNear(root, pawn.Map, 30, c => c.InBounds(pawn.Map) && 
                pawn.CanReach(c, PathEndMode.OnCell, Danger.Deadly, false, TraverseMode.ByPawn), out result, -1) && 
                !CellFinder.TryFindRandomCellNear(pawn.Position, pawn.Map, 5, c => c.InBounds(pawn.Map) &&
                pawn.CanReach(c, PathEndMode.OnCell, Danger.Deadly, false, TraverseMode.ByPawn), out result, -1)))
                result = pawn.Position;
            if (flag)
                pawn.Map.debugDrawer.FlashCell(result, 0.4f, "fallback", 50);
            __result = result;
            return false;
        }
    }
}

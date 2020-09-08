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

namespace RimThreaded
{

    public class RCellFinder_Patch
    {
        public static MethodInfo CanWanderToCell = typeof(RCellFinder).GetMethod("CanWanderToCell", BindingFlags.NonPublic | BindingFlags.Static);

        public static bool RandomWanderDestFor(ref IntVec3 __result, Pawn pawn,
            IntVec3 root,
            float radius,
            Func<Pawn, IntVec3, IntVec3, bool> validator,
            Danger maxDanger)
        {
            if ((double)radius > 12.0)
                Log.Warning("wanderRadius of " + (object)radius + " is greater than Region.GridSize of " + (object)12 + " and will break.", false);
            bool flag = false;
            if (root.GetRegion(pawn.Map, RegionType.Set_Passable) != null)
            {
                int maxRegions = Mathf.Max((int)radius / 3, 13);
                List<Region> regions = new List<Region>();
                CellFinder.AllRegionsNear(regions, root.GetRegion(pawn.Map, RegionType.Set_Passable), maxRegions, TraverseParms.For(pawn, Danger.Deadly, TraverseMode.ByPawn, false), (Predicate<Region>)(reg => (double)reg.extentsClose.ClosestDistSquaredTo(root) <= (double)radius * (double)radius), (Pawn)null, RegionType.Set_Passable);
                if (flag)
                    pawn.Map.debugDrawer.FlashCell(root, 0.6f, nameof(root), 50);
                if (regions.Count > 0)
                {
                    for (int tryIndex = 0; tryIndex < 35; ++tryIndex)
                    {
                        IntVec3 c = IntVec3.Invalid;
                        for (int index = 0; index < 5; ++index)
                        {
                            IntVec3 randomCell = regions.RandomElementByWeight<Region>((Func<Region, float>)(reg => (float)reg.CellCount)).RandomCell;
                            if ((double)randomCell.DistanceToSquared(root) <= (double)radius * (double)radius)
                            {
                                c = randomCell;
                                break;
                            }
                        }
                        if (!c.IsValid)
                        {
                            if (flag)
                                pawn.Map.debugDrawer.FlashCell(c, 0.32f, "distance", 50);
                        }
                        else if (!(bool)CanWanderToCell.Invoke(null, new object[] { c, pawn, root, validator, tryIndex, maxDanger }))
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
            if (!CellFinder.TryFindRandomCellNear(root, pawn.Map, Mathf.FloorToInt(radius), (Predicate<IntVec3>)(c =>
            {
                if (!c.InBounds(pawn.Map) || !pawn.CanReach((LocalTargetInfo)c, PathEndMode.OnCell, Danger.None, false, TraverseMode.ByPawn) || c.IsForbidden(pawn))
                    return false;
                return validator == null || validator(pawn, c, root);
            }), out result, -1) && !CellFinder.TryFindRandomCellNear(root, pawn.Map, Mathf.FloorToInt(radius), (Predicate<IntVec3>)(c => c.InBounds(pawn.Map) && pawn.CanReach((LocalTargetInfo)c, PathEndMode.OnCell, Danger.None, false, TraverseMode.ByPawn) && !c.IsForbidden(pawn)), out result, -1) && (!CellFinder.TryFindRandomCellNear(root, pawn.Map, Mathf.FloorToInt(radius), (Predicate<IntVec3>)(c => c.InBounds(pawn.Map) && pawn.CanReach((LocalTargetInfo)c, PathEndMode.OnCell, Danger.Deadly, false, TraverseMode.ByPawn)), out result, -1) && !CellFinder.TryFindRandomCellNear(root, pawn.Map, 20, (Predicate<IntVec3>)(c => c.InBounds(pawn.Map) && pawn.CanReach((LocalTargetInfo)c, PathEndMode.OnCell, Danger.None, false, TraverseMode.ByPawn) && !c.IsForbidden(pawn)), out result, -1)) && (!CellFinder.TryFindRandomCellNear(root, pawn.Map, 30, (Predicate<IntVec3>)(c => c.InBounds(pawn.Map) && pawn.CanReach((LocalTargetInfo)c, PathEndMode.OnCell, Danger.Deadly, false, TraverseMode.ByPawn)), out result, -1) && !CellFinder.TryFindRandomCellNear(pawn.Position, pawn.Map, 5, (Predicate<IntVec3>)(c => c.InBounds(pawn.Map) && pawn.CanReach((LocalTargetInfo)c, PathEndMode.OnCell, Danger.Deadly, false, TraverseMode.ByPawn)), out result, -1)))
                result = pawn.Position;
            if (flag)
                pawn.Map.debugDrawer.FlashCell(result, 0.4f, "fallback", 50);
            __result = result;
            return false;
        }
    }
}

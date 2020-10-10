using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{

    public class Verb_Patch
    {
        private static bool CanHitFromCellIgnoringRange2(Verb __instance, IntVec3 sourceCell, LocalTargetInfo targ, out IntVec3 goodDest)
        {
            if (targ.Thing != null)
            {
                if (targ.Thing.Map != __instance.caster.Map)
                {
                    goodDest = IntVec3.Invalid;
                    return false;
                }
                List<IntVec3> tempDestList = new List<IntVec3>();
                ShootLeanUtility.CalcShootableCellsOf(tempDestList, targ.Thing);
                for (int i = 0; i < tempDestList.Count; i++)
                {
                    if (CanHitCellFromCellIgnoringRange(__instance, sourceCell, tempDestList[i], targ.Thing.def.Fillage == FillCategory.Full))
                    {
                        goodDest = tempDestList[i];
                        return true;
                    }
                }
            }
            else if (CanHitCellFromCellIgnoringRange(__instance, sourceCell, targ.Cell))
            {
                goodDest = targ.Cell;
                return true;
            }

            goodDest = IntVec3.Invalid;
            return false;
        }

        public static bool TryFindShootLineFromTo(Verb __instance, ref bool __result, IntVec3 root, LocalTargetInfo targ, out ShootLine resultingLine)
        {
            if (targ.HasThing && targ.Thing.Map != __instance.caster.Map)
            {
                resultingLine = default;
                __result = false;
                return false;
            }

            if (__instance.verbProps.IsMeleeAttack || __instance.verbProps.range <= 1.42f)
            {
                resultingLine = new ShootLine(root, targ.Cell);
                __result = ReachabilityImmediate.CanReachImmediate(root, targ, __instance.caster.Map, PathEndMode.Touch, null);
                return false;
            }

            CellRect cellRect = targ.HasThing ? targ.Thing.OccupiedRect() : CellRect.SingleCell(targ.Cell);
            float num = __instance.verbProps.EffectiveMinRange(targ, __instance.caster);
            float num2 = cellRect.ClosestDistSquaredTo(root);
            if (num2 > __instance.verbProps.range * __instance.verbProps.range || num2 < num * num)
            {
                resultingLine = new ShootLine(root, targ.Cell);
                __result = false;
                return false;
            }

            if (!__instance.verbProps.requireLineOfSight)
            {
                resultingLine = new ShootLine(root, targ.Cell);
                __result = true;
                return false;
            }

            IntVec3 goodDest;
            if (__instance.CasterIsPawn)
            {
                if (CanHitFromCellIgnoringRange2(__instance, root, targ, out goodDest))
                {
                    resultingLine = new ShootLine(root, goodDest);
                    __result = true;
                    return false;
                }
                List<IntVec3> tempLeanShootSources = new List<IntVec3>();
                ShootLeanUtility.LeanShootingSourcesFromTo(root, cellRect.ClosestCellTo(root), __instance.caster.Map, tempLeanShootSources);
                for (int i = 0; i < tempLeanShootSources.Count; i++)
                {
                    IntVec3 intVec = tempLeanShootSources[i];
                    if (CanHitFromCellIgnoringRange2(__instance, intVec, targ, out goodDest))
                    {
                        resultingLine = new ShootLine(intVec, goodDest);
                        __result = true;
                        return false;
                    }
                }
            }
            else
            {
                foreach (IntVec3 item in __instance.caster.OccupiedRect())
                {
                    if (CanHitFromCellIgnoringRange2(__instance, item, targ, out goodDest))
                    {
                        resultingLine = new ShootLine(item, goodDest);
                        __result = true;
                        return false;
                    }
                }
            }

            resultingLine = new ShootLine(root, targ.Cell);
            __result = false;
            return false;
        }

        public static bool CanHitFromCellIgnoringRange(Verb __instance, ref bool __result,
          IntVec3 sourceCell,
          LocalTargetInfo targ,
          out IntVec3 goodDest)
        {
            if (targ.Thing != null)
            {
                if (targ.Thing.Map != __instance.caster.Map)
                {
                    goodDest = IntVec3.Invalid;
                    __result = false;
                    return false;
                }
                List<IntVec3> tempDestList = new List<IntVec3>();
                ShootLeanUtility.CalcShootableCellsOf(tempDestList, targ.Thing);
                for (int index = 0; index < tempDestList.Count; ++index)
                {
                    if (CanHitCellFromCellIgnoringRange(__instance, sourceCell, tempDestList[index], targ.Thing.def.Fillage == FillCategory.Full))
                    {
                        goodDest = tempDestList[index];
                        __result = true;
                        return false;
                    }
                }
            }
            else if (CanHitCellFromCellIgnoringRange(__instance, sourceCell, targ.Cell, false))
            {
                goodDest = targ.Cell;
                __result = true;
                return false;
            }
            goodDest = IntVec3.Invalid;
            __result = false;
            return false;
        }

        private static bool CanHitCellFromCellIgnoringRange(Verb __instance,
          IntVec3 sourceSq,
          IntVec3 targetLoc,
          bool includeCorners = false)
        {
            if (__instance.verbProps.mustCastOnOpenGround && (!targetLoc.Standable(__instance.caster.Map) || __instance.caster.Map.thingGrid.CellContains(targetLoc, ThingCategory.Pawn)))
                return false;
            if (__instance.verbProps.requireLineOfSight)
            {
                if (!includeCorners)
                {
                    if (!GenSight.LineOfSight(sourceSq, targetLoc, __instance.caster.Map, true, null, 0, 0))
                        return false;
                }
                else if (!GenSight.LineOfSightToEdges(sourceSq, targetLoc, __instance.caster.Map, true, null))
                    return false;
            }
            return true;
        }


    }
}

using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace RimThreaded
{

    public class Verb_Patch
    {


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
                    if (!GenSight.LineOfSight(sourceSq, targetLoc, __instance.caster.Map, true, (Func<IntVec3, bool>)null, 0, 0))
                        return false;
                }
                else if (!GenSight.LineOfSightToEdges(sourceSq, targetLoc, __instance.caster.Map, true, (Func<IntVec3, bool>)null))
                    return false;
            }
            return true;
        }


    }
}

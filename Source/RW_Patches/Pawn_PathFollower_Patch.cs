using System;
using UnityEngine;
using Verse;
using Verse.AI;

namespace RimThreaded.RW_Patches
{
    class Pawn_PathFollower_Patch
    {
        internal static void RunDestructivePatches()
        {
            Type original = typeof(Pawn_PathFollower);
            Type patched = typeof(Pawn_PathFollower_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(CostToMoveIntoCell), new Type[] { typeof(Pawn), typeof(IntVec3) });
        }

        public static bool CostToMoveIntoCell(ref int __result, Pawn pawn, IntVec3 c)
        {
            int a = (c.x == pawn.Position.x || c.z == pawn.Position.z ? pawn.TicksPerMoveCardinal : pawn.TicksPerMoveDiagonal) + pawn.Map.pathing.For(pawn).pathGrid.CalculatedCostAt(c, false, pawn.Position);
            Building edifice = c.GetEdifice(pawn.Map);
            if (edifice != null)
                a += edifice.PathWalkCostFor(pawn);
            if (a > 450)
                a = 450;
            if (pawn.CurJob != null)
            {
                Pawn locomotionUrgencySameAs = pawn?.jobs?.curDriver?.locomotionUrgencySameAs; //changed
                if (locomotionUrgencySameAs != null && locomotionUrgencySameAs != pawn && locomotionUrgencySameAs.Spawned)
                {
                    int moveIntoCell = Pawn_PathFollower.CostToMoveIntoCell(locomotionUrgencySameAs, c);
                    if (a < moveIntoCell)
                        a = moveIntoCell;
                }
                else
                {
                    switch (pawn.jobs.curJob.locomotionUrgency)
                    {
                        case LocomotionUrgency.Amble:
                            a *= 3;
                            if (a < 60)
                            {
                                a = 60;
                                break;
                            }
                            break;
                        case LocomotionUrgency.Walk:
                            a *= 2;
                            if (a < 50)
                            {
                                a = 50;
                                break;
                            }
                            break;
                        case LocomotionUrgency.Jog:
                            //a = a; //commented out
                            break;
                        case LocomotionUrgency.Sprint:
                            a = Mathf.RoundToInt(a * 0.75f);
                            break;
                    }
                }
            }
            __result = Mathf.Max(a, 1);
            return false;
        }
    }
}

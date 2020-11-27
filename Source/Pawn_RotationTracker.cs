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

    
    public class Pawn_RotationTracker_Patch
	{
        public static FieldRef<Pawn_RotationTracker, Pawn> pawnField = FieldRefAccess<Pawn_RotationTracker, Pawn>("pawn");
        public static bool UpdateRotation(Pawn_RotationTracker __instance)
        {
            Pawn pawn = pawnField(__instance);
            if (pawn.Destroyed || pawn.jobs.HandlingFacing)
            {
                return false;
            }

            if (pawn.pather.Moving)
            {
                if (pawn.pather.curPath != null && pawn.pather.curPath.NodesLeftCount >= 1)
                {
                    FaceAdjacentCell2(pawn, pawn.pather.nextCell);
                }

                return false;
            }

            Stance_Busy stance_Busy = pawn.stances.curStance as Stance_Busy;
            if (stance_Busy != null && stance_Busy.focusTarg.IsValid)
            {
                if (stance_Busy.focusTarg.HasThing)
                {
                    __instance.Face(stance_Busy.focusTarg.Thing.DrawPos);
                }
                else
                {
                    __instance.FaceCell(stance_Busy.focusTarg.Cell);
                }

                return false;
            }

            Job job = pawn.CurJob; //ADDED
            if (job != null) //CHANGED
            {
                LocalTargetInfo target = job.GetTarget(pawn.jobs.curDriver.rotateToFace); //CHANGED
                __instance.FaceTarget(target);
            }

            if (pawn.Drafted)
            {
                pawn.Rotation = Rot4.South;
            }
            return false;
        }
        private static void FaceAdjacentCell2(Pawn pawn, IntVec3 c)
        {
            if (!(c == pawn.Position))
            {
                IntVec3 intVec = c - pawn.Position;
                if (intVec.x > 0)
                {
                    pawn.Rotation = Rot4.East;
                }
                else if (intVec.x < 0)
                {
                    pawn.Rotation = Rot4.West;
                }
                else if (intVec.z > 0)
                {
                    pawn.Rotation = Rot4.North;
                }
                else
                {
                    pawn.Rotation = Rot4.South;
                }
            }
        }
      


    }
}

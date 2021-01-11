using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace RimThreaded
{
    class SickPawnVisitUtility_Patch
    {
        public static bool FindRandomSickPawn(ref Pawn __result, Pawn pawn, JoyCategory maxPatientJoy)
        {
            List<Pawn> pawnList = pawn.Map.mapPawns.FreeColonistsSpawned;
            List<Pawn> source = new List<Pawn>();
            for (int i = 0; i < pawnList.Count; i++)
            {
                Pawn pawn2;
                try
                {
                    pawn2 = pawnList[i];
                } catch(ArgumentOutOfRangeException)
                {
                    break;
                }
                if (pawn2 != null && SickPawnVisitUtility.CanVisit(pawn, pawn2, maxPatientJoy)) {
                    source.Add(pawn2);
                }
            }
            Func<Pawn, float> weightSelector = (Pawn x) => VisitChanceScore(pawn, x);
            bool result2 = GenCollection_Patch.TryRandomElementByWeight_Pawn(source, weightSelector, out Pawn result);
            if (!result2)
            {
                __result = null;
                return false;
            }

            __result = result;
            return false;
        }
        private static float VisitChanceScore(Pawn pawn, Pawn sick)
        {
            float num = GenMath.LerpDouble(-100f, 100f, 0.05f, 2f, pawn.relations.OpinionOf(sick));
            float lengthHorizontal = (pawn.Position - sick.Position).LengthHorizontal;
            float num2 = Mathf.Clamp(GenMath.LerpDouble(0f, 150f, 1f, 0.2f, lengthHorizontal), 0.2f, 1f);
            return num * num2;
        }
    }
}

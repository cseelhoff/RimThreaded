using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using System.Collections.Concurrent;

namespace RimThreaded
{

    public class ShootLeanUtility_Patch
    {

		public static ConcurrentQueue<bool[]> conBlockedArrays = new ConcurrentQueue<bool[]>();

		public static void CalcShootableCellsOf(List<IntVec3> outCells, Thing t)
		{
			outCells.Clear();
			if (t is Pawn)
			{
				outCells.Add(t.Position);
				for (int i = 0; i < 4; i++)
				{
					IntVec3 intVec = t.Position + GenAdj.CardinalDirections[i];
					if (intVec.CanBeSeenOver(t.Map))
					{
						outCells.Add(intVec);
					}
				}
				return;
			}
			outCells.Add(t.Position);
			if (t.def.size.x != 1 || t.def.size.z != 1)
			{
				foreach (IntVec3 intVec2 in t.OccupiedRect())
				{
					if (intVec2 != t.Position)
					{
						outCells.Add(intVec2);
					}
				}
			}
		}
		private static void ReturnWorkingBlockedArray(bool[] ar)
		{
			conBlockedArrays.Enqueue(ar);
			if (conBlockedArrays.Count > 128)
			{
				Log.ErrorOnce("Too many blocked arrays to be feasible. >128", 388121, false);
			}
		}
		private static bool[] GetWorkingBlockedArray()
		{
            if (!conBlockedArrays.TryDequeue(out bool[] boolArray))
            {
                boolArray = new bool[8];
            }
            return boolArray;
		}
		public static bool LeanShootingSourcesFromTo(IntVec3 shooterLoc, IntVec3 targetPos, Map map, List<IntVec3> listToFill)
		{
			lock (listToFill)
			{
				listToFill.Clear();
			}
			float angleFlat = (targetPos - shooterLoc).AngleFlat;
			bool flag = angleFlat > 270f || angleFlat < 90f;
			bool flag2 = angleFlat > 90f && angleFlat < 270f;
			bool flag3 = angleFlat > 180f;
			bool flag4 = angleFlat < 180f;
			bool[] workingBlockedArray = GetWorkingBlockedArray();
			for (int i = 0; i < 8; i++)
			{
				workingBlockedArray[i] = !(shooterLoc + GenAdj.AdjacentCells[i]).CanBeSeenOver(map);
			}
			if (!workingBlockedArray[1] && ((workingBlockedArray[0] && !workingBlockedArray[5] && flag) || (workingBlockedArray[2] && !workingBlockedArray[4] && flag2)))
			{
				lock (listToFill)
				{
					listToFill.Add(shooterLoc + new IntVec3(1, 0, 0));
				}
			}
			if (!workingBlockedArray[3] && ((workingBlockedArray[0] && !workingBlockedArray[6] && flag) || (workingBlockedArray[2] && !workingBlockedArray[7] && flag2)))
			{
				lock (listToFill)
				{
					listToFill.Add(shooterLoc + new IntVec3(-1, 0, 0));
				}
			}
			if (!workingBlockedArray[2] && ((workingBlockedArray[3] && !workingBlockedArray[7] && flag3) || (workingBlockedArray[1] && !workingBlockedArray[4] && flag4)))
			{
				lock (listToFill)
				{
					listToFill.Add(shooterLoc + new IntVec3(0, 0, -1));
				}
			}
			if (!workingBlockedArray[0] && ((workingBlockedArray[3] && !workingBlockedArray[6] && flag3) || (workingBlockedArray[1] && !workingBlockedArray[5] && flag4)))
			{
				lock (listToFill)
				{
					listToFill.Add(shooterLoc + new IntVec3(0, 0, 1));
				}
			}
			for (int j = 0; j < 4; j++)
			{
				if (!workingBlockedArray[j] && (j != 0 || flag) && (j != 1 || flag4) && (j != 2 || flag2) && (j != 3 || flag3) && (shooterLoc + GenAdj.AdjacentCells[j]).GetCover(map) != null)
				{
					lock (listToFill)
					{
						listToFill.Add(shooterLoc + GenAdj.AdjacentCells[j]);
					}
				}
			}
			ReturnWorkingBlockedArray(workingBlockedArray);
			return false;
		}

	}
}

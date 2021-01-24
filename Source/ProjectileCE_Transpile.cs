using HarmonyLib;
using RimWorld;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static HarmonyLib.AccessTools;
using static RimThreaded.RimThreadedHarmony;

namespace RimThreaded
{
	class ProjectileCE_Transpile
    {
        public static List<Thing> CheckCellForCollision2(List<Thing> thingsListAtFast)
        {
            //List<Thing> list = new List<Thing>(map.thingGrid.ThingsListAtFast(cell)).Where((Thing t) => justWallsRoofs ? (t.def.Fillage == FillCategory.Full) : (t is Pawn || t.def.Fillage != FillCategory.None)).ToList();
            List<Thing> returnList = new List<Thing>();
            for (int i = 0; i < thingsListAtFast.Count; i++)
            {
                Thing t;
                try
                {
                    t = thingsListAtFast[i];
                } 
				catch (ArgumentOutOfRangeException)
                {
                    break;
                }
                if (t is Pawn || t.def.Fillage != FillCategory.None)
                {
                    returnList.Add(t);
                }
            }
            return returnList;
        }
		static MethodInfo methodTryCollideWith =
			Method(combatExtended_ProjectileCE, "dTryCollideWith", new Type[] { typeof(Thing) });
		static Func<object, Thing, bool> funcTryCollideWith =
			(Func<object, Thing, bool>)Delegate.CreateDelegate(typeof(Func<object, Thing, bool>), methodTryCollideWith);

		static MethodInfo methodApplySuppression =
			Method(typeof(LongEventHandler), "ApplySuppression", new Type[] { typeof(Pawn) });
		static Action<Pawn> actionApplySuppression =
			(Action<Pawn>)Delegate.CreateDelegate(typeof(Action<Pawn>), methodApplySuppression);

		public static bool CheckCellForCollision3(object projectileCE, List<Thing> list, Vector3 LastPos, Thing launcher, Thing mount, bool canTargetSelf, 
			bool flag2, Thing intendedTarget, Vector3 ExactPosition)
		{
			foreach (Thing item2 in from x in list.Distinct()
									orderby (x.DrawPos - LastPos).sqrMagnitude
									select x)
			{
				if ((item2 == launcher || item2 == mount) && !canTargetSelf)
				{
					continue;
				}
				if ((!flag2 || item2 == intendedTarget) && funcTryCollideWith(projectileCE, item2))
				{
					return true;
				}
				//if (justWallsRoofs)
				//{
					//continue;
				//}
				Vector3 exactPosition = ExactPosition;
				if (exactPosition.y < 3f)
				{
					Pawn pawn = item2 as Pawn;
					if (pawn != null)
					{
						actionApplySuppression(pawn);
					}
				}
			}
			return false;
		}

		public static bool CheckForCollisionBetween2(IOrderedEnumerable<IntVec3> orderedEnumerable)
        {
			//foreach (IntVec3 intVec3 in orderedEnumerable)
			//for(int i = 0; i < orderedEnumerable.Count)
			//{
				//if (this.CheckCellForCollision(intVec3))
				//{
					//return true;
				//}
				/*
				if (Controller.settings.DebugDrawInterceptChecks)
				{
					base.Map.debugDrawer.FlashCell(intVec3, 1f, "o", 50);
				}
				*/
			//}
			return false;
		}

		public static IEnumerable<CodeInstruction> CheckCellForCollision(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
		{
			int[] matchesFound = new int[1]; //EDIT
			List<CodeInstruction> instructionsList = instructions.ToList();
			int i = 0;
			while (i < instructionsList.Count)
			{
				int matchIndex = 0;
				if (
					instructionsList[i].opcode == OpCodes.Newobj && //EDIT
					(ConstructorInfo)instructionsList[i].operand == Constructor(typeof(List<Thing>), new Type[] { typeof(IEnumerable<Thing>) }) //EDIT
				)
				{
					instructionsList[i].opcode = OpCodes.Call;
					instructionsList[i].operand = Method(typeof(ProjectileCE_Transpile), "CheckCellForCollision2");
					while (i < instructionsList.Count)
					{
						if (
							instructionsList[i].opcode == OpCodes.Stloc_S && //EDIT
							((LocalBuilder)instructionsList[i].operand).LocalIndex == 4 //EDIT
						)
							break;
						i++;
					}
					matchesFound[matchIndex]++;
					continue;
				}
				yield return instructionsList[i++];
			}
			for (int mIndex = 0; mIndex < matchesFound.Length; mIndex++)
			{
				if (matchesFound[mIndex] < 1)
					Log.Error("IL code instruction set " + mIndex + " not found");
			}
		}


		public static IEnumerable<CodeInstruction> CheckForCollisionBetween(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
		{
			int[] matchesFound = new int[1]; //EDIT
			List<CodeInstruction> instructionsList = instructions.ToList();
			int i = 0;
			while (i < instructionsList.Count)
			{
				int matchIndex = 0;
				if (
					instructionsList[i].opcode == OpCodes.Newobj && //EDIT
					(ConstructorInfo)instructionsList[i].operand == Constructor(typeof(List<Thing>), new Type[] { typeof(IEnumerable<Thing>) }) //EDIT
				)
				{
					instructionsList[i].opcode = OpCodes.Call;
					instructionsList[i].operand = Method(typeof(ProjectileCE_Transpile), "CheckCellForCollision2");
					while (i < instructionsList.Count)
					{
						if (
							instructionsList[i].opcode == OpCodes.Stloc_S && //EDIT
							((LocalBuilder)instructionsList[i].operand).LocalIndex == 4 //EDIT
						)
							break;
						i++;
					}
					matchesFound[matchIndex]++;
					continue;
				}
				yield return instructionsList[i++];
			}
			for (int mIndex = 0; mIndex < matchesFound.Length; mIndex++)
			{
				if (matchesFound[mIndex] < 1)
					Log.Error("IL code instruction set " + mIndex + " not found");
			}
		}
	}
}

using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Verse;
using static HarmonyLib.AccessTools;
using static RimThreaded.RimThreadedHarmony;

namespace RimThreaded
{
    class Hauling_Transpile
    {
		public static IEnumerable<CodeInstruction> CanHaul(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
		{
			int[] matchesFound = new int[1]; //EDIT
			Type dictionary_Thing_IntVec3 = typeof(Dictionary<Thing, IntVec3>); //EDIT
			List<CodeInstruction> instructionsList = instructions.ToList();
			int i = 0;
			while (i < instructionsList.Count)
			{
				int matchIndex = 0;
				if (
					i + 5 < instructionsList.Count && //EDIT
					instructionsList[i].opcode == OpCodes.Ldsfld && //EDIT
					(FieldInfo)instructionsList[i].operand == cachedStoreCell && //EDIT
					instructionsList[i + 5].opcode == OpCodes.Call //EDIT
					)
				{
					List<CodeInstruction> loadLockObjectInstructions = new List<CodeInstruction>
				{
					new CodeInstruction(OpCodes.Ldsfld, cachedStoreCell) //EDIT
				};
					LocalBuilder lockObject = iLGenerator.DeclareLocal(dictionary_Thing_IntVec3); //EDIT
					LocalBuilder lockTaken = iLGenerator.DeclareLocal(typeof(bool));
					foreach (CodeInstruction ci in EnterLock(lockObject, lockTaken, loadLockObjectInstructions, instructionsList, ref i))
						yield return ci;

					while (i < instructionsList.Count)
					{
						if (
						instructionsList[i - 1].opcode == OpCodes.Call //EDIT
						)
							break;
						yield return instructionsList[i++];
					}
					foreach (CodeInstruction ci in ExitLock(iLGenerator, lockObject, lockTaken, instructionsList, ref i))
						yield return ci;
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

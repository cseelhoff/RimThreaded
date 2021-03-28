using HarmonyLib;
using System.Collections.Generic;
using Verse;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using System;
using static RimThreaded.RimThreadedHarmony;
using static HarmonyLib.AccessTools;
using RimWorld;

namespace RimThreaded
{
    public class GenLabel_Transpile
    {
		public static IEnumerable<CodeInstruction> ThingLabel(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
		{
			int[] matchesFound = new int[1]; //EDIT
			Type labelRequest = TypeByName("RimWorld.GenLabel+LabelRequest"); //EDIT
			Type dictionary_LabelRequest_String = 
				typeof(Dictionary<,>).MakeGenericType(new Type[] { labelRequest, typeof(string) }); //EDIT
			List <CodeInstruction> instructionsList = instructions.ToList();
			int i = 0;
			while (i < instructionsList.Count)
			{
				int matchIndex = 0;
				if (
					i + 2 < instructionsList.Count && //EDIT
					instructionsList[i].opcode == OpCodes.Ldsfld && //EDIT
					(FieldInfo)instructionsList[i].operand == Field(typeof(GenLabel), "labelDictionary") && //EDIT
					instructionsList[i + 2].opcode == OpCodes.Ldloca_S //EDIT
					)
				{
					List<CodeInstruction> loadLockObjectInstructions = new List<CodeInstruction>
					{
						new CodeInstruction(OpCodes.Ldsfld, Field(typeof(GenLabel), "labelDictionary")) //EDIT
					};
					LocalBuilder lockObject = iLGenerator.DeclareLocal(dictionary_LabelRequest_String); //EDIT
					LocalBuilder lockTaken = iLGenerator.DeclareLocal(typeof(bool));
					foreach (CodeInstruction ci in EnterLock(
						lockObject, lockTaken, loadLockObjectInstructions, instructionsList[i]))
						yield return ci;

					while (i < instructionsList.Count)
					{
						if (
						instructionsList[i - 1].opcode == OpCodes.Callvirt && //EDIT
						(MethodInfo)instructionsList[i - 1].operand == Method(dictionary_LabelRequest_String, "Add") //EDIT
						)
							break;
						yield return instructionsList[i++];
					}
					foreach (CodeInstruction ci in ExitLock(
						iLGenerator, lockObject, lockTaken, instructionsList[i]))
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

using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Verse;
using System.Reflection.Emit;
using System;
using Verse.AI;
using static HarmonyLib.AccessTools;
using RimWorld;
using System.Reflection;

namespace RimThreaded
{
    public class GenLabel_Transpile
    {
        public static IEnumerable<CodeInstruction> ThingLabel(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
			List<CodeInstruction> instructionsList = instructions.ToList();

			int i = 0;
			int matchesFound = 0;

			while (i < instructionsList.Count)
			{
				if (i + 2 < instructionsList.Count && 
					instructionsList[i].opcode == OpCodes.Ldsfld &&
					(FieldInfo)instructionsList[i].operand == Field(typeof(GenLabel), "labelDictionary") &&
					instructionsList[i+2].opcode == OpCodes.Ldloca_S
					)
				{
					
				}
				else
				{
					yield return instructionsList[i];
					i++;
				}
			}
			if (matchesFound < 1)
			{
				Log.Error("IL code instructions not found");
			}
		}      
    }
}

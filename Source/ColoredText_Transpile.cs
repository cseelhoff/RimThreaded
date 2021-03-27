using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Verse;
using System.Reflection.Emit;
using System.Reflection;
using System;

namespace RimThreaded
{

    public class ColoredText_Transpile
	{
		public static IEnumerable<CodeInstruction> Resolve(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
		{
			List<CodeInstruction> instructionsList = instructions.ToList();
			int i = 0;
			int matchesFound = 0;
			while (i < instructionsList.Count)
			{
				if (instructionsList[i].opcode == OpCodes.Callvirt &&
					(MethodInfo)instructionsList[i].operand == AccessTools.Method(typeof(Dictionary<string,string>), "Add")
					)
				{
					instructionsList[i].operand = AccessTools.Method(typeof(Dictionary<string, string>), "set_Item");
					yield return instructionsList[i++];
					matchesFound++;
				}
				else
				{
					yield return instructionsList[i++];
				}
			}
			if (matchesFound < 1)
			{
				Log.Error("IL code instructions not found");
			}
		}

        internal static void RunNonDestructivePatches()
		{
			Type original = typeof(ColoredText);
			Type patched = typeof(ColoredText_Transpile);
			RimThreadedHarmony.Transpile(original, patched, "Resolve");
		}
    }
}

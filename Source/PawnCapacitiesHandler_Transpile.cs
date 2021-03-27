
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using static HarmonyLib.AccessTools;
using static RimThreaded.RimThreadedHarmony;
using System.Reflection;
using System.Reflection.Emit;
using static RimThreaded.PawnCapacitiesHandler_Patch;
using Verse;

namespace RimThreaded
{
    class PawnCapacitiesHandler_Transpile
    {
		public static IEnumerable<CodeInstruction> GetLevel(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
		{
			int[] matchesFound = new int[1]; //EDIT
			List<CodeInstruction> instructionsList = instructions.ToList();
			LocalBuilder cacheElement = iLGenerator.DeclareLocal(typeof(CacheElement2));
			int i = 0;
			while (i < instructionsList.Count)
			{
				int matchIndex = 0;
				if (
					i + 2 < instructionsList.Count &&
					instructionsList[i + 1].opcode == OpCodes.Ldfld &&
					(FieldInfo)instructionsList[i+1].operand == Field(typeof(PawnCapacitiesHandler), "cachedCapacityLevels") &&
					instructionsList[i + 2].opcode == OpCodes.Brtrue_S
					)
				{
					instructionsList[i].opcode = OpCodes.Ldarg_0;
					instructionsList[i].operand = null;
					yield return instructionsList[i++];
					yield return new CodeInstruction(OpCodes.Ldarg_1);
					yield return new CodeInstruction(OpCodes.Call, Method(typeof(PawnCapacitiesHandler_Patch), "getCacheElementResult"));
					yield return new CodeInstruction(OpCodes.Ret);
					matchesFound[matchIndex]++;
					break;
				}
				yield return instructionsList[i++];
			}
			for (int mIndex = 0; mIndex < matchesFound.Length; mIndex++)
			{
				if (matchesFound[mIndex] < 1)
					Log.Error("IL code instruction set " + mIndex + " not found");
			}
		}

        internal static void RunNonDestructivePatches()
		{
			Type original = typeof(PawnCapacitiesHandler);
			Type patched = typeof(PawnCapacitiesHandler_Transpile);
			RimThreadedHarmony.Transpile(original, patched, "GetLevel");
		}
    }
}

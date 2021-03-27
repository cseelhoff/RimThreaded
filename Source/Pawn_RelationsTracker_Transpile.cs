using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using static HarmonyLib.AccessTools;
using System.Reflection;
using RimWorld;
using Verse;
using System;

namespace RimThreaded
{
	class Pawn_RelationsTracker_Transpile
	{
		public static IEnumerable<CodeInstruction> ReplacePotentiallyRelatedPawns(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
		{
			int[] matchesFound = new int[1];
			List<CodeInstruction> instructionsList = instructions.ToList();
			int i = 0;
			while (i < instructionsList.Count)
			{
				int matchIndex = 0;
				if (
					(instructionsList[i].opcode == OpCodes.Callvirt || instructionsList[i].opcode == OpCodes.Call) &&
					(MethodInfo)instructionsList[i].operand == Method(typeof(Pawn_RelationsTracker), "get_PotentiallyRelatedPawns")
					)
				{
					instructionsList[i].operand = Method(typeof(Pawn_RelationsTracker_Patch), "get_PotentiallyRelatedPawns2");
					matchesFound[matchIndex]++;
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
			Type original = typeof(FocusStrengthOffset_GraveCorpseRelationship);
			Type patched = typeof(Pawn_RelationsTracker_Transpile);
			MethodInfo pMethod = Method(patched, "ReplacePotentiallyRelatedPawns");
			RimThreadedHarmony.harmony.Patch(Method(original, "CanApply"), transpiler: new HarmonyMethod(pMethod));
			//Pawn_RelationsTracker.get_RelatedPawns
			original = TypeByName("RimWorld.Pawn_RelationsTracker+<get_RelatedPawns>d__30");
			RimThreadedHarmony.harmony.Patch(Method(original, "MoveNext"), transpiler: new HarmonyMethod(pMethod));
			//Pawn_RelationsTracker
			original = typeof(Pawn_RelationsTracker);
			//Pawn_RelationsTracker.Notify_PawnKilled
			RimThreadedHarmony.harmony.Patch(Method(original, "Notify_PawnKilled"), transpiler: new HarmonyMethod(pMethod));
			//Pawn_RelationsTracker.Notify_PawnSold
			RimThreadedHarmony.harmony.Patch(Method(original, "Notify_PawnSold"), transpiler: new HarmonyMethod(pMethod));
			//PawnDiedOrDownedThoughtsUtility.AppendThoughts_Relations
			original = typeof(PawnDiedOrDownedThoughtsUtility);
			pMethod = Method(patched, "ReplacePotentiallyRelatedPawns");
			RimThreadedHarmony.harmony.Patch(Method(original, "AppendThoughts_Relations"), transpiler: new HarmonyMethod(pMethod));
		}
    }
}

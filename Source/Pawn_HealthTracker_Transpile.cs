using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using static HarmonyLib.AccessTools;
using System.Reflection;
using RimWorld;
using Verse;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;

namespace RimThreaded
{
    class Pawn_HealthTracker_Transpile
    {
		public static IEnumerable<CodeInstruction> RemoveHediff(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
		{
			/*
			 * 
			 * 	// if (__instance.hediffSet == null || __instance.hediffSet.hediffs == null)
				IL_0000: ldarg.0
				IL_0001: ldfld class ['Assembly-CSharp']Verse.HediffSet ['Assembly-CSharp']Verse.Pawn_HealthTracker::hediffSet
				IL_0006: brfalse.s IL_0015

				IL_0008: ldarg.0
				IL_0009: ldfld class ['Assembly-CSharp']Verse.HediffSet ['Assembly-CSharp']Verse.Pawn_HealthTracker::hediffSet
				IL_000e: ldfld class [mscorlib]System.Collections.Generic.List`1<class ['Assembly-CSharp']Verse.Hediff> ['Assembly-CSharp']Verse.HediffSet::hediffs
				IL_0013: brfalse.s IL_0017

				// return false;
				IL_0015: ldc.i4.0
				IL_0016: ret
			 * 
			 */
			List<CodeInstruction> instructionsList = instructions.ToList();
			int i = 0;
			yield return new CodeInstruction(OpCodes.Ldarg_0);
			yield return new CodeInstruction(OpCodes.Ldfld, Field(typeof(Pawn_HealthTracker), "hediffSet"));
			Label labelEnd = iLGenerator.DefineLabel();
			yield return new CodeInstruction(OpCodes.Brfalse_S, labelEnd);
			yield return new CodeInstruction(OpCodes.Ldarg_0);
			yield return new CodeInstruction(OpCodes.Ldfld, Field(typeof(Pawn_HealthTracker), "hediffSet"));
			yield return new CodeInstruction(OpCodes.Ldfld, Field(typeof(HediffSet), "hediffs"));
			yield return new CodeInstruction(OpCodes.Brfalse_S, labelEnd);

			while (i < instructionsList.Count - 1)
			{
				yield return instructionsList[i++];
			}
			instructionsList[i].labels.Add(labelEnd);
			yield return instructionsList[i];
		}

	}
}

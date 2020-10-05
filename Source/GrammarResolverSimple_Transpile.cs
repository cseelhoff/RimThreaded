using HarmonyLib;
using System.Collections.Generic;
using Verse;
using System.Reflection;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using System.Reflection.Emit;
using System.Linq;
using System;
using System.Threading;
using Verse.AI;

namespace RimThreaded
{
    public class GrammarResolverSimple_Transpile
    {
        public static IEnumerable<CodeInstruction> Formatted(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
			/* Original PathFinder.FindPath
			
			---START ORIG---	IL_000b: ldsfld bool Verse.GrammarResolverSimple::working
			
			---END ORIG---

			---START TARGET---
			IL_0000: ldc-i4-1
			---END TARGET---


			*/
			//LocalBuilder tmpCells = iLGenerator.DeclareLocal(typeof(List<IntVec3>));
            int i = 0;
			//yield return new CodeInstruction(OpCodes.Newobj, typeof(List<IntVec3>).GetConstructor(Type.EmptyTypes));
			//yield return new CodeInstruction(OpCodes.Stloc_S, tmpCells.LocalIndex);

			List<CodeInstruction> instructionsList = instructions.ToList();
			while (i < instructionsList.Count)
			{
				//---START ORIG-- -
				//	IL_000b: ldsfld bool Verse.GrammarResolverSimple::working
				//-- - END ORIG-- -

				//---START TARGET-- -
				//IL_0000: Ldc_I4_0
				//-- - END TARGET-- -
				if (
					instructionsList[i].opcode == OpCodes.Ldsfld &&
					(FieldInfo)instructionsList[i].operand == AccessTools.Field(typeof(GrammarResolverSimple), "working")
					)
				{
					instructionsList[i].opcode = OpCodes.Ldc_I4_1;
					instructionsList[i].operand = null;
					yield return instructionsList[i];
				}
				else
				{
					yield return instructionsList[i];
				}
                i++;
            }
		}
    }
}

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
    public class GenRadial_Transpile
    {
        public static IEnumerable<CodeInstruction> ProcessEquidistantCells(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
			/* Original PathFinder.FindPath
			
			---START ORIG---
			IL_0000: ldsfld bool Verse.GenRadial::working
			---END ORIG---

			---START TARGET---
			IL_0000: ld_0
			---END TARGET---


			*/
			LocalBuilder tmpCells = iLGenerator.DeclareLocal(typeof(List<IntVec3>));
            int i = 0;
			yield return new CodeInstruction(OpCodes.Newobj, typeof(List<IntVec3>).GetConstructor(Type.EmptyTypes));
			yield return new CodeInstruction(OpCodes.Stloc_S, tmpCells.LocalIndex);

			List<CodeInstruction> instructionsList = instructions.ToList();
			while (i < instructionsList.Count)
			{
				//---START ORIG-- -
				//IL_0000: ldsfld bool Verse.GenRadial::working
				//-- - END ORIG-- -

				//---START TARGET-- -
				//IL_0000: Ldc_I4_0
				//-- - END TARGET-- -
				if (
					instructionsList[i].opcode == OpCodes.Ldsfld &&
					(FieldInfo)instructionsList[i].operand == AccessTools.Field(typeof(GenRadial), "working")
					)
				{
					instructionsList[i].opcode = OpCodes.Ldc_I4_0;
					instructionsList[i].operand = null;
					yield return instructionsList[i];
				}
				else if (
						instructionsList[i].opcode == OpCodes.Ldsfld && (FieldInfo)instructionsList[i].operand == AccessTools.Field(typeof(GenRadial), "tmpCells")
					)
				{
					//ldloc   (tmpCells.LocalIndex)
					instructionsList[i].opcode = OpCodes.Ldloc;
					instructionsList[i].operand = tmpCells.LocalIndex;
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

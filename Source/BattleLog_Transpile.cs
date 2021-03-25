using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System;
using static HarmonyLib.AccessTools;
using static RimThreaded.RimThreadedHarmony;

namespace RimThreaded
{
    public class BattleLog_Transpile
    {
		public static object addLogEntryLock = new object();
		public static IEnumerable<CodeInstruction> Add(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
			List<CodeInstruction> instructionsList = instructions.ToList();
			int i = 0;
			Type lockObjectType = typeof(object);
			List<CodeInstruction> loadLockObjectInstructions = new List<CodeInstruction>
			{
				new CodeInstruction(OpCodes.Ldsfld, Field(typeof(BattleLog_Transpile), "addLogEntryLock")),
			};
			LocalBuilder lockObject = iLGenerator.DeclareLocal(lockObjectType);
			LocalBuilder lockTaken = iLGenerator.DeclareLocal(typeof(bool));
			foreach (CodeInstruction ci in EnterLock(
				lockObject, lockTaken, loadLockObjectInstructions, instructionsList, ref i))
				yield return ci;

			while (i < instructionsList.Count - 1)
			{
				yield return instructionsList[i++];
			}
			foreach (CodeInstruction ci in ExitLock(
				iLGenerator, lockObject, lockTaken, instructionsList, ref i))
				yield return ci;

			while (i < instructionsList.Count)
			{
				yield return instructionsList[i++];
			}			
		}
    }
}

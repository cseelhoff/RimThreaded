using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using Verse;
using static RimThreaded.RimThreadedHarmony;

namespace RimThreaded
{
    class TickList_Transpile
    {
        public static IEnumerable<CodeInstruction> DeregisterThing(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            List<CodeInstruction> instructionsList = instructions.ToList();
            int i = 0;
            List<CodeInstruction> loadLockObjectInstructions = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldarg_0)
            };
            LocalBuilder lockObject = iLGenerator.DeclareLocal(typeof(object));
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
            yield return instructionsList[i++];
        }
        public static IEnumerable<CodeInstruction> RegisterThing(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            List<CodeInstruction> instructionsList = instructions.ToList();
            int i = 0;
            List<CodeInstruction> loadLockObjectInstructions = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldarg_0)
            };
            LocalBuilder lockObject = iLGenerator.DeclareLocal(typeof(object));
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
            yield return instructionsList[i++];
        }

        internal static void RunNonDestructivePatches()
        {
            Type original = typeof(TickList);
            Type patched = typeof(TickList_Transpile);
            Transpile(original, patched, "RegisterThing");
            Transpile(original, patched, "DeregisterThing");
        }
    }
}

using HarmonyLib;
using System.Collections.Generic;
using Verse;
using System.Reflection.Emit;
using System.Linq;
using System;
using System.Threading;
using System.Reflection;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{
    public class ThingOwnerThing_Transpile
    {
        internal static void RunNonDestructivePatches()
        {
            Type original = typeof(ThingOwner<Thing>);
            Type patched = typeof(ThingOwnerThing_Transpile);
            RimThreadedHarmony.TranspileLockAdd3(original, "TryAdd", new Type[] { typeof(Thing), typeof(bool) });
            RimThreadedHarmony.Transpile(original, patched, "Remove");
        }
        public static IEnumerable<CodeInstruction> Remove(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
			List<CodeInstruction> instructionsList = instructions.ToList();
			int i = 0;
			while (i < instructionsList.Count)
			{
				if (i + 9 < instructionsList.Count && instructionsList[i + 9].opcode == OpCodes.Callvirt)
				{
					if (instructionsList[i + 9].operand is MethodInfo methodInfo)
					{
						if (methodInfo.Name.Contains("RemoveAt") && methodInfo.DeclaringType.FullName.Contains("System.Collections"))
						{
							LocalBuilder lockObject = iLGenerator.DeclareLocal(typeof(object));
							LocalBuilder lockTaken = iLGenerator.DeclareLocal(typeof(bool));
							List<CodeInstruction> loadLockObjectInstructions = new List<CodeInstruction>()
							{
								new CodeInstruction(OpCodes.Ldarg_0)
							};
							foreach (CodeInstruction lockInstruction in RimThreadedHarmony.EnterLock(lockObject, lockTaken, loadLockObjectInstructions, instructionsList[i]))
							{
								yield return lockInstruction;
							}
							Type collectionType = methodInfo.DeclaringType;
							LocalBuilder collectionCopy = iLGenerator.DeclareLocal(collectionType);
							ConstructorInfo constructorInfo = collectionType.GetConstructor(new Type[] { collectionType });
							List<CodeInstruction> storeReplay = new List<CodeInstruction>();
							storeReplay.Add(instructionsList[i]);
							yield return instructionsList[i++]; //this
							storeReplay.Add(instructionsList[i]);
							yield return instructionsList[i++]; //load field
							yield return new CodeInstruction(OpCodes.Newobj, constructorInfo);
							yield return new CodeInstruction(OpCodes.Stloc, collectionCopy.LocalIndex);
							yield return new CodeInstruction(OpCodes.Ldloc, collectionCopy.LocalIndex);
							yield return instructionsList[i++]; //load item
							yield return instructionsList[i++]; //unbox
							yield return instructionsList[i++]; //last index of
							yield return instructionsList[i++]; //store to loc 0
							i++;//yield return instructionsList[i++]; //this
							i++;//yield return instructionsList[i++]; //load field
							yield return new CodeInstruction(OpCodes.Ldloc, collectionCopy.LocalIndex);
							yield return instructionsList[i++]; //load loc 0
							yield return instructionsList[i++]; //removeAt (void)
							int j = 0;
							while(j < storeReplay.Count - 1)
                            {
								yield return storeReplay[j++];
							}
							yield return new CodeInstruction(OpCodes.Ldloc, collectionCopy.LocalIndex);
							yield return new CodeInstruction(OpCodes.Stfld, storeReplay[j].operand);

							foreach (CodeInstruction lockInstruction in RimThreadedHarmony.ExitLock(iLGenerator, lockObject, lockTaken, instructionsList[i]))
							{
								yield return lockInstruction;
							}
							continue;
						}
					}
				}
				yield return instructionsList[i++];
			}
		}

    }
}

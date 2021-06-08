using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Verse;
using System.Reflection.Emit;
using System;
using System.Reflection;
using UnityEngine;
using RimThreaded.Mod_Patches;

namespace RimThreaded
{
    public class JobGiver_FindItemByRadius_Transpile
    {
        public static IEnumerable<CodeInstruction> Reset(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            LocalBuilder tmpRadius = iLGenerator.DeclareLocal(typeof(List<int>));
            List<CodeInstruction> instructionsList = instructions.ToList();
            int currentInstructionIndex = 0;
            int matchFound = 0;
            yield return new CodeInstruction(OpCodes.Newobj, AccessTools.Constructor(typeof(List<int>)));
            yield return new CodeInstruction(OpCodes.Stloc, tmpRadius.LocalIndex);
            while (currentInstructionIndex < instructionsList.Count)
            {
                CodeInstruction codeInstruction = instructionsList[currentInstructionIndex];
                if (currentInstructionIndex + 1 < instructionsList.Count &&
                    instructionsList[currentInstructionIndex + 1].opcode == OpCodes.Ldfld &&
                    (FieldInfo)instructionsList[currentInstructionIndex + 1].operand == AccessTools.Field(AwesomeInventory_Patch.awesomeInventoryJobsJobGiver_FindItemByRadius, "_radius"))
                {
                    matchFound++;
                    codeInstruction.opcode = OpCodes.Ldloc;
                    codeInstruction.operand = tmpRadius.LocalIndex;
                    currentInstructionIndex++;
                }
                else if (currentInstructionIndex == instructionsList.Count - 1)
                {
                    yield return new CodeInstruction(OpCodes.Ldloc, tmpRadius.LocalIndex);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Stfld, AccessTools.Field(AwesomeInventory_Patch.awesomeInventoryJobsJobGiver_FindItemByRadius, "_radius"));
                    yield return instructionsList[currentInstructionIndex];
                    currentInstructionIndex++;
                }
                yield return codeInstruction;
                currentInstructionIndex++;
            }
            if (matchFound < 4)
            {
                Log.Error("IL code instructions not found");
            }
        }
        public static IEnumerable<CodeInstruction> FindItem(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            LocalBuilder itemFound = iLGenerator.DeclareLocal(typeof(bool));
            LocalBuilder lastUsedRadius = iLGenerator.DeclareLocal(typeof(int));
            LocalBuilder lengthHorizontal = iLGenerator.DeclareLocal(typeof(float));
            LocalBuilder intvec3 = iLGenerator.DeclareLocal(typeof(IntVec3));
            LocalBuilder radius = iLGenerator.DeclareLocal(typeof(int[]));
            List<CodeInstruction> instructionsList = instructions.ToList();
            int currentInstructionIndex = 0;
            int matchFound = 0;
            Label IL_00b2 = iLGenerator.DefineLabel();
            Label IL_0122 = iLGenerator.DefineLabel();
            while (currentInstructionIndex < instructionsList.Count)
            {
                if (currentInstructionIndex + 3 < instructionsList.Count &&
                    instructionsList[currentInstructionIndex + 3].opcode == OpCodes.Call &&
                    (MethodInfo)instructionsList[currentInstructionIndex + 3].operand == AccessTools.Method(AwesomeInventory_Patch.awesomeInventoryJobsJobGiver_FindItemByRadius, "Reset")
                    )
                {
                    matchFound++;
                    // float lengthHorizontal = pawn.Map.Size.LengthHorizontal;
                    instructionsList[currentInstructionIndex].opcode = OpCodes.Ldloc_0;
                    instructionsList[currentInstructionIndex].operand = null;
                    yield return instructionsList[currentInstructionIndex];
                    //yield return new CodeInstruction(OpCodes.Ldloc_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(AwesomeInventory_Patch.awesomeInventoryJobsJobGiver_FindItemByRadiusSub, "pawn"));
                    yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Thing), "get_Map"));
                    yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Map), "get_Size"));
                    yield return new CodeInstruction(OpCodes.Stloc_S, intvec3.LocalIndex);
                    yield return new CodeInstruction(OpCodes.Ldloca_S, intvec3.LocalIndex);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(IntVec3), "get_LengthHorizontal"));
                    yield return new CodeInstruction(OpCodes.Stloc, lengthHorizontal.LocalIndex);

                    // 	int[] array = new int[3]
                    // 	{
                    // 		Mathf.FloorToInt(lengthHorizontal * _tinyRadiusFactor),
                    // 		Mathf.FloorToInt(lengthHorizontal * _smallRadiusFactor),
                    // 		Mathf.FloorToInt(lengthHorizontal * _mediumRadiusFactor)
                    // 	};
                    yield return new CodeInstruction(OpCodes.Ldc_I4_3);
                    yield return new CodeInstruction(OpCodes.Newarr, typeof(int));
                    yield return new CodeInstruction(OpCodes.Dup);
                    yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                    yield return new CodeInstruction(OpCodes.Ldloc, lengthHorizontal.LocalIndex);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(AwesomeInventory_Patch.awesomeInventoryJobsJobGiver_FindItemByRadius, "_tinyRadiusFactor"));
                    yield return new CodeInstruction(OpCodes.Mul);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Mathf), "FloorToInt", new Type[] { typeof(float) }));
                    yield return new CodeInstruction(OpCodes.Stelem_I4);
                    yield return new CodeInstruction(OpCodes.Dup);
                    yield return new CodeInstruction(OpCodes.Ldc_I4_1);
                    yield return new CodeInstruction(OpCodes.Ldloc, lengthHorizontal.LocalIndex);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(AwesomeInventory_Patch.awesomeInventoryJobsJobGiver_FindItemByRadius, "_smallRadiusFactor"));
                    yield return new CodeInstruction(OpCodes.Mul);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Mathf), "FloorToInt", new Type[] { typeof(float) }));
                    yield return new CodeInstruction(OpCodes.Stelem_I4);
                    yield return new CodeInstruction(OpCodes.Dup);
                    yield return new CodeInstruction(OpCodes.Ldc_I4_2);
                    yield return new CodeInstruction(OpCodes.Ldloc, lengthHorizontal.LocalIndex);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(AwesomeInventory_Patch.awesomeInventoryJobsJobGiver_FindItemByRadius, "_mediumRadiusFactor"));
                    yield return new CodeInstruction(OpCodes.Mul);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Mathf), "FloorToInt", new Type[] { typeof(float) }));
                    yield return new CodeInstruction(OpCodes.Stelem_I4);
                    yield return new CodeInstruction(OpCodes.Stloc, radius.LocalIndex);

                    // int lastUsedRadius = _lastUsedRadiusIndex;
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(AwesomeInventory_Patch.awesomeInventoryJobsJobGiver_FindItemByRadius, "_lastUsedRadiusIndex"));
                    yield return new CodeInstruction(OpCodes.Stloc, lastUsedRadius.LocalIndex);

                    // if (_itemFound)
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(AwesomeInventory_Patch.awesomeInventoryJobsJobGiver_FindItemByRadius, "_itemFound"));
                    yield return new CodeInstruction(OpCodes.Stloc, itemFound.LocalIndex);
                    yield return new CodeInstruction(OpCodes.Ldloc, itemFound.LocalIndex);                    
                    yield return new CodeInstruction(OpCodes.Brfalse_S, IL_00b2);

                    // lastUsedRadius--;
                    yield return new CodeInstruction(OpCodes.Ldloc, lastUsedRadius.LocalIndex);
                    yield return new CodeInstruction(OpCodes.Ldc_I4_1);
                    yield return new CodeInstruction(OpCodes.Sub);
                    yield return new CodeInstruction(OpCodes.Stloc, itemFound.LocalIndex);

                    currentInstructionIndex += 24;
                } else if (currentInstructionIndex + 1 < instructionsList.Count &&
                    instructionsList[currentInstructionIndex].opcode == OpCodes.Ldnull &&
                    instructionsList[currentInstructionIndex + 1].opcode == OpCodes.Stloc_1
                )
                {
                    matchFound++;
                    // if (lastUsedRadius < 0)
                    CodeInstruction codeInstruction = new CodeInstruction(OpCodes.Ldloc, lastUsedRadius.LocalIndex);
                    codeInstruction.labels.Add(IL_00b2);
                    yield return codeInstruction;
                    yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                    Label IL_00b8 = iLGenerator.DefineLabel();
                    yield return new CodeInstruction(OpCodes.Bge_S, IL_00b8);

                    // lastUsedRadius = 0;
                    yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                    yield return new CodeInstruction(OpCodes.Stloc, lastUsedRadius.LocalIndex);

                    // bool itemFound = false;
                    codeInstruction = new CodeInstruction(OpCodes.Ldc_I4_0);
                    codeInstruction.labels.Add(IL_00b8);
                    yield return codeInstruction;
                    yield return new CodeInstruction(OpCodes.Stloc, itemFound.LocalIndex);
                    yield return instructionsList[currentInstructionIndex];
                    currentInstructionIndex++;
                }
                //_radius[_lastUsedRadiusIndex]
                else if (currentInstructionIndex + 4 < instructionsList.Count &&
                   instructionsList[currentInstructionIndex + 4].opcode == OpCodes.Callvirt &&
                   (MethodInfo)instructionsList[currentInstructionIndex + 4].operand == AccessTools.Method(typeof(List<int>), "get_Item")
               )
                {
                    matchFound++;
                    // array[num]
                    yield return new CodeInstruction(OpCodes.Ldloc, radius.LocalIndex);
                    yield return new CodeInstruction(OpCodes.Ldloc, lastUsedRadius.LocalIndex);
                    yield return new CodeInstruction(OpCodes.Ldelem_I4);
                    currentInstructionIndex += 5;
                }
                // _itemFound = true;
                else if (currentInstructionIndex + 2 < instructionsList.Count &&
                   instructionsList[currentInstructionIndex + 1].opcode == OpCodes.Ldc_I4_1 &&
                   instructionsList[currentInstructionIndex + 2].opcode == OpCodes.Stfld &&
                   (FieldInfo)instructionsList[currentInstructionIndex + 2].operand == AccessTools.Field(AwesomeInventory_Patch.awesomeInventoryJobsJobGiver_FindItemByRadius, "_itemFound")
               )
                {
                    matchFound++;
                    // itemFound = true;
                    instructionsList[currentInstructionIndex].opcode = OpCodes.Ldc_I4_1;
                    instructionsList[currentInstructionIndex].operand = null;
                    yield return instructionsList[currentInstructionIndex];
                    yield return new CodeInstruction(OpCodes.Stloc, itemFound.LocalIndex);
                    currentInstructionIndex += 3;
                }
                // if (++_lastUsedRadiusIndex == _radius.Count)
                // _lastUsedRadiusIndex = DefaultRadiusIndex;
                else if (currentInstructionIndex + 11 < instructionsList.Count &&
                   instructionsList[currentInstructionIndex + 11].opcode == OpCodes.Callvirt &&
                   (MethodInfo)instructionsList[currentInstructionIndex + 11].operand == AccessTools.Method(typeof(List<int>), "get_Count")
               )
                {
                    matchFound++;
                    // num++;
                    yield return new CodeInstruction(OpCodes.Ldloc, lastUsedRadius.LocalIndex);
                    yield return new CodeInstruction(OpCodes.Ldc_I4_1);
                    yield return new CodeInstruction(OpCodes.Add);
                    yield return new CodeInstruction(OpCodes.Stloc, lastUsedRadius.LocalIndex);

                    // if (num >= _radius.Count)
                    yield return new CodeInstruction(OpCodes.Ldloc, lastUsedRadius.LocalIndex);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(AwesomeInventory_Patch.awesomeInventoryJobsJobGiver_FindItemByRadius, "_radius"));
                    yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(List<int>), "get_Count"));
                    yield return new CodeInstruction(OpCodes.Blt_S, IL_0122);
                    // num = DefaultRadiusIndex;
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(AwesomeInventory_Patch.awesomeInventoryJobsJobGiver_FindItemByRadius, "get_DefaultRadiusIndex"));
                    yield return new CodeInstruction(OpCodes.Stloc, lastUsedRadius.LocalIndex);
                    currentInstructionIndex += 16;
                }
                else if (currentInstructionIndex + 3 < instructionsList.Count &&
                  instructionsList[currentInstructionIndex + 1].opcode == OpCodes.Brtrue_S &&
                  instructionsList[currentInstructionIndex + 3].opcode == OpCodes.Dup
              )
                {
                    matchFound++;
                    instructionsList[currentInstructionIndex].labels.Add(IL_0122);
                    yield return instructionsList[currentInstructionIndex];
                    currentInstructionIndex++;
                }
                else if (currentInstructionIndex == instructionsList.Count - 2)
                {
                    matchFound++;
                    CodeInstruction codeInstruction = new CodeInstruction(OpCodes.Ldarg_0);
                    codeInstruction.labels = instructionsList[currentInstructionIndex].labels;
                    instructionsList[currentInstructionIndex].labels = new List<Label>();
                    yield return codeInstruction;
                    //yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldloc, lastUsedRadius.LocalIndex);
                    yield return new CodeInstruction(OpCodes.Stfld, AccessTools.Field(AwesomeInventory_Patch.awesomeInventoryJobsJobGiver_FindItemByRadius, "_lastUsedRadiusIndex"));

                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldloc, itemFound.LocalIndex);
                    yield return new CodeInstruction(OpCodes.Stfld, AccessTools.Field(AwesomeInventory_Patch.awesomeInventoryJobsJobGiver_FindItemByRadius, "_itemFound"));
                    yield return instructionsList[currentInstructionIndex];
                    currentInstructionIndex++;
                }
                else
                {
                    yield return instructionsList[currentInstructionIndex];
                    currentInstructionIndex++;
                }
            }
            if (matchFound < 7)
            {
                Log.Error("IL code instructions not found");
            }
        
        }

        public static IEnumerable<CodeInstruction> FindItem2(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            LocalBuilder lastUsedRadius = iLGenerator.DeclareLocal(typeof(int));
            List<CodeInstruction> instructionsList = instructions.ToList();
            int currentInstructionIndex = 0;
            int matchFound = 0;
            while (currentInstructionIndex < instructionsList.Count)
            {
                if (currentInstructionIndex + 2 < instructionsList.Count &&
                    instructionsList[currentInstructionIndex + 1].opcode == OpCodes.Ldfld &&
                    (FieldInfo)instructionsList[currentInstructionIndex + 1].operand == AccessTools.Field(AwesomeInventory_Patch.awesomeInventoryJobsJobGiver_FindItemByRadius, "_itemFound") &&
                    instructionsList[currentInstructionIndex + 2].opcode == OpCodes.Brfalse_S)
                {
                    matchFound++;

                    // int num = Math.Min(_lastUsedRadiusIndex, _radius.Count - 1);
                    //IL_004d: ldsfld int32 RimThreaded.JobGiver_FindItemByRadius_Patch::_lastUsedRadiusIndex
                    //IL_0052: ldsfld class [mscorlib] System.Collections.Generic.List`1<int32> RimThreaded.JobGiver_FindItemByRadius_Patch::_radius
                    //IL_0057: callvirt instance int32 class [mscorlib] System.Collections.Generic.List`1<int32>::get_Count()
                    //IL_005c: ldc.i4.1
                    //IL_005d: sub
                    //IL_005e: call int32[mscorlib]System.Math::Min(int32, int32)
                    //IL_0063: stloc.1
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(AwesomeInventory_Patch.awesomeInventoryJobsJobGiver_FindItemByRadius, "_lastUsedRadiusIndex"));
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(AwesomeInventory_Patch.awesomeInventoryJobsJobGiver_FindItemByRadius, "_radius"));
                    yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(List<int>), "get_Count"));
                    yield return new CodeInstruction(OpCodes.Ldc_I4_1);
                    yield return new CodeInstruction(OpCodes.Sub);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Math), "Min", new Type[] { typeof(int), typeof(int) }));
                    yield return new CodeInstruction(OpCodes.Stloc, lastUsedRadius.LocalIndex);

                    //if (this._itemFound)
                    yield return instructionsList[currentInstructionIndex];
                    currentInstructionIndex++;
                    yield return instructionsList[currentInstructionIndex];
                    currentInstructionIndex++;
                    yield return instructionsList[currentInstructionIndex];
                    currentInstructionIndex++;
                
                    // if (--local_lastUsedRadiusIndex < 0)
                    //IL_0070: ldloc.1
                    //IL_0071: ldc.i4.1
                    //IL_0072: sub
                    //IL_0073: dup
                    //IL_0074: stloc.1
                    //IL_0075: ldc.i4.0
                    //IL_007c: bge.s IL_0082

                    yield return new CodeInstruction(OpCodes.Ldloc, lastUsedRadius.LocalIndex);
                    yield return new CodeInstruction(OpCodes.Ldc_I4_1);
                    yield return new CodeInstruction(OpCodes.Sub);
                    yield return new CodeInstruction(OpCodes.Stloc, lastUsedRadius.LocalIndex);

                    //if (--_lastUsedRadiusIndex < 0)
                    // _lastUsedRadiusIndex++;
                    currentInstructionIndex += 17;
                }
                else if (currentInstructionIndex + 1 < instructionsList.Count &&
                    instructionsList[currentInstructionIndex].opcode == OpCodes.Ldnull &&
                    instructionsList[currentInstructionIndex + 1].opcode == OpCodes.Stloc_1
                    )
                {
                    yield return new CodeInstruction(OpCodes.Ldloc, lastUsedRadius.LocalIndex);
                    yield return new CodeInstruction(OpCodes.Ldc_I4_1);
                    Label bge = iLGenerator.DefineLabel();
                    yield return new CodeInstruction(OpCodes.Bge_S, bge);
                    yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                    yield return new CodeInstruction(OpCodes.Stloc, lastUsedRadius.LocalIndex);
                    instructionsList[currentInstructionIndex].labels.Add(bge);
                    yield return instructionsList[currentInstructionIndex];
                    currentInstructionIndex++;
                }
                //_radius[_lastUsedRadiusIndex]
                else if (currentInstructionIndex + 2 < instructionsList.Count &&
                 instructionsList[currentInstructionIndex + 1].opcode == OpCodes.Ldfld &&
                 (FieldInfo)instructionsList[currentInstructionIndex + 1].operand == AccessTools.Field(AwesomeInventory_Patch.awesomeInventoryJobsJobGiver_FindItemByRadius, "_lastUsedRadiusIndex") &&
                 instructionsList[currentInstructionIndex + 2].opcode == OpCodes.Callvirt &&
                 (MethodInfo)instructionsList[currentInstructionIndex + 2].operand == AccessTools.Method(typeof(List<int>), "get_Item")
                 )
                {
                    matchFound++;
                    //_radius[local_lastUsedRadiusIndex]
                    instructionsList[currentInstructionIndex].opcode = OpCodes.Ldloc;
                    instructionsList[currentInstructionIndex].operand = lastUsedRadius.LocalIndex;
                    yield return instructionsList[currentInstructionIndex];
                    currentInstructionIndex+=2;
                }
                // if (++_lastUsedRadiusIndex == _radius.Count)
                else if (currentInstructionIndex + 4 < instructionsList.Count &&
                 instructionsList[currentInstructionIndex].opcode == OpCodes.Ldarg_0 &&
                 instructionsList[currentInstructionIndex + 1].opcode == OpCodes.Ldarg_0 &&
                 instructionsList[currentInstructionIndex + 2].opcode == OpCodes.Ldfld &&
                 (FieldInfo)instructionsList[currentInstructionIndex + 2].operand == AccessTools.Field(AwesomeInventory_Patch.awesomeInventoryJobsJobGiver_FindItemByRadius, "_lastUsedRadiusIndex") &&
                 instructionsList[currentInstructionIndex + 3].opcode == OpCodes.Ldc_I4_1 &&
                 instructionsList[currentInstructionIndex + 4].opcode == OpCodes.Add
                 )
                {
                    matchFound++;
                    //if (++local_lastUsedRadiusIndex == this._radius.Count)
                    //IL_00f6: ldloc.1
                    //IL_00f7: ldc.i4.1
                    //IL_00f8: add
                    //IL_00f9: dup
                    //IL_00fa: stloc.1
                    instructionsList[currentInstructionIndex].opcode = OpCodes.Ldloc;
                    instructionsList[currentInstructionIndex].operand = lastUsedRadius.LocalIndex;
                    yield return instructionsList[currentInstructionIndex];
                    currentInstructionIndex++;
                    yield return new CodeInstruction(OpCodes.Ldc_I4_1);
                    yield return new CodeInstruction(OpCodes.Add);
                    yield return new CodeInstruction(OpCodes.Dup);
                    yield return new CodeInstruction(OpCodes.Stloc, lastUsedRadius.LocalIndex);

                    /*
		            IL_00de: ldarg.0
		            IL_00df: ldfld int32 AwesomeInventory.Jobs.JobGiver_FindItemByRadius::_lastUsedRadiusIndex
		            IL_00e4: ldc.i4.1
		            IL_00e5: add
		            IL_00e6: stloc.2
		            IL_00e7: ldloc.2
		            IL_00e8: stfld int32 AwesomeInventory.Jobs.JobGiver_FindItemByRadius::_lastUsedRadiusIndex
		            IL_00ed: ldloc.2
                    */
                    currentInstructionIndex += 8;

                    //IL_00ee: ldarg.0
                    yield return instructionsList[currentInstructionIndex];
                    currentInstructionIndex++;

                    //IL_00ef: ldfld class [mscorlib]System.Collections.Generic.List`1<int32> AwesomeInventory.Jobs.JobGiver_FindItemByRadius::_radius
                    yield return instructionsList[currentInstructionIndex];
                    currentInstructionIndex++;

                    //IL_00f4: callvirt instance int32 class [mscorlib]System.Collections.Generic.List`1<int32>::get_Count()
                    yield return instructionsList[currentInstructionIndex];
                    currentInstructionIndex++;

                    //IL_00f9: bne.un.s IL_0111
                    yield return instructionsList[currentInstructionIndex];
                    currentInstructionIndex++;

                    // local_lastUsedRadiusIndex = DefaultRadiusIndex;
                    //IL_00fb: ldarg.0
                    currentInstructionIndex++;

                    //IL_00fc: call int32 AwesomeInventory.Jobs.JobGiver_FindItemByRadius::get_DefaultRadiusIndex()
                    yield return instructionsList[currentInstructionIndex];
                    currentInstructionIndex++;
                    instructionsList[currentInstructionIndex].opcode = OpCodes.Stloc;
                    instructionsList[currentInstructionIndex].operand = lastUsedRadius.LocalIndex;
                    yield return instructionsList[currentInstructionIndex];
                    currentInstructionIndex++;

                    //yield return instructionsList[currentInstructionIndex];
                    // _lastUsedRadiusIndex = DefaultRadiusIndex;
                    //currentInstructionIndex += 2;

                }
                else if (currentInstructionIndex == instructionsList.Count - 2)
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldloc, lastUsedRadius.LocalIndex);
                    yield return new CodeInstruction(OpCodes.Stfld, AccessTools.Field(AwesomeInventory_Patch.awesomeInventoryJobsJobGiver_FindItemByRadius, "_lastUsedRadiusIndex"));
                    yield return instructionsList[currentInstructionIndex];
                    currentInstructionIndex++;
                }
                else
                {
                    yield return instructionsList[currentInstructionIndex];
                    currentInstructionIndex++;
                }
            }
            if (matchFound < 3)
            {
                Log.Error("IL code instructions not found");
            }
        }
    }
}

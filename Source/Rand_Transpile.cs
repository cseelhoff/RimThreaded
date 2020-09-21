using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Threading;
using Verse;

namespace RimThreaded
{
    public class Rand_Transpile
    {

        public static IEnumerable<CodeInstruction> TryRangeInclusiveWhere(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            LocalBuilder tmpRange = iLGenerator.DeclareLocal(typeof(List<int>));
            List<CodeInstruction> instructionsList = instructions.ToList<CodeInstruction>();
            int i = 0;
            while (i < instructionsList.Count)
            {
                if (
                    instructionsList[i].opcode == OpCodes.Ldarg_0 &&
                    instructionsList[i+1].opcode == OpCodes.Ldfld && instructionsList[i+1].operand.ToString().Equals("System.Collections.Generic.List`1[Verse.Thing][] thingGrid") &&
                    instructionsList[i+2].opcode == OpCodes.Ldarg_0 &&
                    instructionsList[i+3].opcode == OpCodes.Ldfld && instructionsList[i+3].operand.ToString().Equals("Verse.Map map") &&
                    instructionsList[i+4].opcode == OpCodes.Ldfld && instructionsList[i+4].operand.ToString().Equals("Verse.CellIndices cellIndices") &&
                    instructionsList[i+5].opcode == OpCodes.Ldarg_2 && 
                    instructionsList[i+6].opcode == OpCodes.Callvirt && instructionsList[i+6].operand.ToString().Equals("Int32 CellToIndex(Verse.IntVec3)") &&
                    instructionsList[i+7].opcode == OpCodes.Ldelem_Ref && 
                    instructionsList[i+8].opcode == OpCodes.Ldarg_1 && 
                    instructionsList[i+9].opcode == OpCodes.Callvirt && instructionsList[i+9].operand.ToString().Equals("Void Add(Verse.Thing)")
                    )
                {
                    yield return instructionsList[i];
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ThingGrid), "thingGrid"));
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ThingGrid), "map"));
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Map), "cellIndices"));
                    yield return new CodeInstruction(OpCodes.Ldarg_2);
                    yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(CellIndices), "CellToIndex", new Type[] { typeof(IntVec3) }));
                    yield return new CodeInstruction(OpCodes.Ldelem_Ref);
                    //yield return new CodeInstruction(OpCodes.Stloc, monitorLockObject.LocalIndex);
                    yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                    //yield return new CodeInstruction(OpCodes.Stloc, lockTaken.LocalIndex);
                    //yield return new CodeInstruction(OpCodes.Ldloc, monitorLockObject.LocalIndex);
                    //yield return new CodeInstruction(OpCodes.Ldloca_S, lockTaken.LocalIndex);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Monitor), "Enter", new Type[] { typeof(object), typeof(bool).MakeByRefType() }));
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    break;
                }
                else
                {
                    yield return instructionsList[i];
                }
                i++;
            }
            while (i < instructionsList.Count)
            {
                if (
                    instructionsList[i-10].opcode == OpCodes.Ldarg_0 &&
                    instructionsList[i-9].opcode == OpCodes.Ldfld && instructionsList[i-9].operand.ToString().Equals("System.Collections.Generic.List`1[Verse.Thing][] thingGrid") &&
                    instructionsList[i-8].opcode == OpCodes.Ldarg_0 &&
                    instructionsList[i-7].opcode == OpCodes.Ldfld && instructionsList[i-7].operand.ToString().Equals("Verse.Map map") &&
                    instructionsList[i-6].opcode == OpCodes.Ldfld && instructionsList[i-6].operand.ToString().Equals("Verse.CellIndices cellIndices") &&
                    instructionsList[i-5].opcode == OpCodes.Ldarg_2 &&
                    instructionsList[i-4].opcode == OpCodes.Callvirt && instructionsList[i-4].operand.ToString().Equals("Int32 CellToIndex(Verse.IntVec3)") &&
                    instructionsList[i-3].opcode == OpCodes.Ldelem_Ref &&
                    instructionsList[i-2].opcode == OpCodes.Ldarg_1 &&
                    instructionsList[i-1].opcode == OpCodes.Callvirt && instructionsList[i-1].operand.ToString().Equals("Void Add(Verse.Thing)")
                    )
                {
                    Label labelRet = iLGenerator.DefineLabel();
                    yield return new CodeInstruction(OpCodes.Leave_S, labelRet);
                    //yield return new CodeInstruction(OpCodes.Ldloc, lockTaken.LocalIndex);
                    Label labelEndFinally = iLGenerator.DefineLabel();
                    yield return new CodeInstruction(OpCodes.Brfalse_S, labelEndFinally);
                    //yield return new CodeInstruction(OpCodes.Ldloc, monitorLockObject.LocalIndex);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Monitor), "Exit"));
                    CodeInstruction ciEndFinally = new CodeInstruction(OpCodes.Endfinally);
                    ciEndFinally.labels.Add(labelEndFinally);
                    yield return ciEndFinally;
                    instructionsList[i].labels.Add(labelRet);
                    break;
                }
                else
                {
                    yield return instructionsList[i];
                }
                i++;
            }
            while (i < instructionsList.Count)
            {
                yield return instructionsList[i];
                i++;
            }
        }

        public static IEnumerable<CodeInstruction> DeregisterInCell(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            LocalBuilder monitorLockObject = iLGenerator.DeclareLocal(typeof(List<Thing>));
            LocalBuilder lockTaken = iLGenerator.DeclareLocal(typeof(bool));
            List<CodeInstruction> instructionsList = instructions.ToList<CodeInstruction>();
            int i = 0;
            Label labelLeave = iLGenerator.DefineLabel();
            while (i < instructionsList.Count)
            {
                if (
                    instructionsList[i].opcode == OpCodes.Ldarg_0 &&
                    instructionsList[i + 1].opcode == OpCodes.Ldfld && instructionsList[i + 1].operand.ToString().Equals("System.Collections.Generic.List`1[Verse.Thing][] thingGrid") &&
                    instructionsList[i + 2].opcode == OpCodes.Ldloc_0 &&
                    instructionsList[i + 3].opcode == OpCodes.Ldelem_Ref && 
                    instructionsList[i + 4].opcode == OpCodes.Ldarg_1 && 
                    instructionsList[i + 5].opcode == OpCodes.Callvirt && instructionsList[i + 5].operand.ToString().Equals("Boolean Contains(Verse.Thing)") &&
                    instructionsList[i + 6].opcode == OpCodes.Brfalse_S
                    )
                {
                    yield return instructionsList[i];
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ThingGrid), "thingGrid"));
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ThingGrid), "map"));
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Map), "cellIndices"));
                    yield return new CodeInstruction(OpCodes.Ldarg_2);
                    yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(CellIndices), "CellToIndex", new Type[] { typeof(IntVec3) }));
                    yield return new CodeInstruction(OpCodes.Ldelem_Ref);
                    yield return new CodeInstruction(OpCodes.Stloc, monitorLockObject.LocalIndex);
                    yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                    yield return new CodeInstruction(OpCodes.Stloc, lockTaken.LocalIndex);
                    yield return new CodeInstruction(OpCodes.Ldloc, monitorLockObject.LocalIndex);
                    yield return new CodeInstruction(OpCodes.Ldloca_S, lockTaken.LocalIndex);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Monitor), "Enter", new Type[] { typeof(object), typeof(bool).MakeByRefType() }));
                    yield return new CodeInstruction(OpCodes.Ldarg_0);                    
                    instructionsList[i + 6].operand = labelLeave;
                    break;
                }
                else
                {
                    yield return instructionsList[i];
                }
                i++;
            }
            while (i < instructionsList.Count)
            {
                if (
                    instructionsList[i-7].opcode == OpCodes.Ldarg_0 &&
                    instructionsList[i-6].opcode == OpCodes.Ldfld && instructionsList[i-6].operand.ToString().Equals("System.Collections.Generic.List`1[Verse.Thing][] thingGrid") &&
                    instructionsList[i-5].opcode == OpCodes.Ldloc_0 &&
                    instructionsList[i-4].opcode == OpCodes.Ldelem_Ref &&
                    instructionsList[i-3].opcode == OpCodes.Ldarg_1 &&
                    instructionsList[i-2].opcode == OpCodes.Callvirt && instructionsList[i-2].operand.ToString().Equals("Boolean Remove(Verse.Thing)") &&
                    instructionsList[i-1].opcode == OpCodes.Pop
                    )
                {
                    Label labelRet = iLGenerator.DefineLabel();
                    CodeInstruction ciLeave = new CodeInstruction(OpCodes.Leave_S, labelRet);
                    ciLeave.labels.Add(labelLeave);
                    yield return ciLeave;
                    yield return new CodeInstruction(OpCodes.Ldloc, lockTaken.LocalIndex);
                    Label labelEndFinally = iLGenerator.DefineLabel();
                    yield return new CodeInstruction(OpCodes.Brfalse_S, labelEndFinally);
                    yield return new CodeInstruction(OpCodes.Ldloc, monitorLockObject.LocalIndex);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Monitor), "Exit"));
                    CodeInstruction ciEndFinally = new CodeInstruction(OpCodes.Endfinally);
                    ciEndFinally.labels.Add(labelEndFinally);
                    yield return ciEndFinally;
                    instructionsList[i].labels.Add(labelRet);
                    break;
                }
                else
                {
                    yield return instructionsList[i];
                }
                i++;
            }
            while (i < instructionsList.Count)
            {
                yield return instructionsList[i];
                i++;
            }
        }

        public static IEnumerable<CodeInstruction> ThingsAt(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            LocalBuilder monitorLockObject = iLGenerator.DeclareLocal(typeof(List<Thing>));
            LocalBuilder lockTaken = iLGenerator.DeclareLocal(typeof(bool));
            List<CodeInstruction> instructionsList = instructions.ToList<CodeInstruction>();
            int i = 0;
            while (i < instructionsList.Count)
            {
                if (
                    instructionsList[i].opcode == OpCodes.Ldarg_0 &&
                    instructionsList[i + 1].opcode == OpCodes.Ldfld && instructionsList[i + 1].operand.ToString().Equals("System.Collections.Generic.List`1[Verse.Thing][] thingGrid") &&
                    instructionsList[i + 2].opcode == OpCodes.Ldloc_0 &&
                    instructionsList[i + 3].opcode == OpCodes.Ldelem_Ref &&
                    instructionsList[i + 4].opcode == OpCodes.Ldarg_1 &&
                    instructionsList[i + 6].opcode == OpCodes.Callvirt && instructionsList[i + 6].operand.ToString().Equals("virtual System.Boolean System.Collections.Generic.List`1<Verse.Thing>::Contains(Verse.Thing item)") &&
                    instructionsList[i + 7].opcode == OpCodes.Brfalse_S
                    )
                {
                    yield return instructionsList[i];
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ThingGrid), "thingGrid"));
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ThingGrid), "map"));
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Map), "cellIndices"));
                    yield return new CodeInstruction(OpCodes.Ldarg_2);
                    yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(CellIndices), "CellToIndex", new Type[] { typeof(IntVec3) }));
                    yield return new CodeInstruction(OpCodes.Ldelem_Ref);
                    yield return new CodeInstruction(OpCodes.Stloc, monitorLockObject.LocalIndex);
                    yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                    yield return new CodeInstruction(OpCodes.Stloc, lockTaken.LocalIndex);
                    yield return new CodeInstruction(OpCodes.Ldloc, monitorLockObject.LocalIndex);
                    yield return new CodeInstruction(OpCodes.Ldloca_S, lockTaken.LocalIndex);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Monitor), "Enter", new Type[] { typeof(object), typeof(bool).MakeByRefType() }));
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    break;
                }
                else
                {
                    yield return instructionsList[i];
                }
                i++;
            }
            while (i < instructionsList.Count)
            {
                if (
                    instructionsList[i - 7].opcode == OpCodes.Ldarg_0 &&
                    instructionsList[i - 6].opcode == OpCodes.Ldfld && instructionsList[i - 6].operand.ToString().Equals("System.Collections.Generic.List`1[Verse.Thing][] thingGrid") &&
                    instructionsList[i - 5].opcode == OpCodes.Ldloc_0 &&
                    instructionsList[i - 4].opcode == OpCodes.Ldelem_Ref &&
                    instructionsList[i - 3].opcode == OpCodes.Ldarg_1 &&
                    instructionsList[i - 2].opcode == OpCodes.Callvirt && instructionsList[i - 2].operand.ToString().Equals("virtual System.Boolean System.Collections.Generic.List`1<Verse.Thing>::Remove(Verse.Thing item)") &&
                    instructionsList[i - 1].opcode == OpCodes.Pop
                    )
                {
                    Label labelRet = new Label();
                    yield return new CodeInstruction(OpCodes.Leave_S, labelRet);
                    yield return new CodeInstruction(OpCodes.Ldloc, lockTaken.LocalIndex);
                    Label labelEndFinally = new Label();
                    yield return new CodeInstruction(OpCodes.Brfalse_S, labelEndFinally);
                    yield return new CodeInstruction(OpCodes.Ldloc, monitorLockObject.LocalIndex);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Monitor), "Exit"));
                    CodeInstruction ciEndFinally = new CodeInstruction(OpCodes.Endfinally);
                    ciEndFinally.labels.Add(labelEndFinally);
                    instructionsList[i].labels.Add(labelRet);
                    break;
                }
                else
                {
                    yield return instructionsList[i];
                }
                i++;
            }
            while (i < instructionsList.Count)
            {
                yield return instructionsList[i];
                i++;
            }
        }

    }

}

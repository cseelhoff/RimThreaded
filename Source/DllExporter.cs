using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.Sound;
using System.Reflection;
using System.Reflection.Emit;

namespace RimThreaded
{

    public class DllExporter
	{
		public static LocalBuilder[] existingVariables;
		public static AssemblyName aName;
		public static AssemblyBuilder ab;
		public static ModuleBuilder modb;
		public static MethodBuilder methb;
		public static TypeBuilder tb;
		public static ILGenerator iLGenerator;
		public static void Init()
		{
			aName = new AssemblyName("RimThreadedHarmonyOutput");
			ab = AppDomain.CurrentDomain.DefineDynamicAssembly(aName, AssemblyBuilderAccess.RunAndSave);
			modb = ab.DefineDynamicModule(aName.Name, aName.Name + ".dll");
			tb = modb.DefineType("PathFinder", TypeAttributes.Public);
			methb = tb.DefineMethod("FindPath", MethodAttributes.Public, typeof(PawnPath), new Type[] { typeof(IntVec3), typeof(TraverseParms) });
			//FieldBuilder fb1 = tb.DefineField("field1", typeof(int), FieldAttributes.Public);

			iLGenerator = methb.GetILGenerator();
			/*
			foreach(LocalBuilder eVar in existingVariables)
            {
				_ = tb.DefineField(eVar.LocalIndex.ToString(), eVar.LocalType, FieldAttributes.Public);
			}
			*/
			//foreach (CodeInstruction codeInstruction in codeInstructions)
			//{
			//iLGenerator.Emit(codeInstruction.opcode, codeInstruction.operand);
			//}
			//iLGenerator.Emit(OpCodes.Ldfld, fb1);
			//iLGenerator.Emit(OpCodes.Ret);

			//_ = tb.CreateType();
			//ab.Save(aName.Name + ".dll");
		}

    }
}

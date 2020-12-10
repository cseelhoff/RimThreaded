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
			/*
            AssemblyName assemblyName = new AssemblyName("RimWorld");
            AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
			ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name, assemblyName.Name + ".dll");
			TypeBuilder typeBuilder = moduleBuilder.DefineType("PathFinder", TypeAttributes.Public);
			MethodBuilder methodBuilder = typeBuilder.DefineMethod("FindPath", MethodAttributes.Public, typeof(PawnPath), new Type[] { typeof(IntVec3), typeof(TraverseParms) });
			FieldBuilder fieldBuilder = typeBuilder.DefineField("field1", typeof(int), FieldAttributes.Public);
			iLGenerator = methodBuilder.GetILGenerator();
			iLGenerator.Emit(OpCodes.Ldfld, fieldBuilder);
			iLGenerator.Emit(OpCodes.Ret);
			typeBuilder.CreateType();
			assemblyBuilder.Save(aName.Name + ".dll");
			*/
		}

    }
}

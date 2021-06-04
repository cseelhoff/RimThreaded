using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;
using System.Reflection.Emit;
using System.Threading;
using RimThreaded.Mod_Patches;
using RimWorld;
using UnityEngine;
using Verse.Sound;
using static HarmonyLib.AccessTools;
using Verse.AI;
using System.IO;
using Newtonsoft.Json;

namespace RimThreaded
{

	[StaticConstructorOnStartup]
	public class RimThreadedHarmony
	{
		public static Harmony harmony = new Harmony("majorhoff.rimthreaded");

		public static FieldInfo cachedStoreCell;
		public static HashSet<MethodInfo> nonDestructivePrefixes = new HashSet<MethodInfo>();

		static RimThreadedHarmony()
		{
#if DEBUG
			Harmony.DEBUG = true;
#endif
			Log.Message("RimThreaded " + Assembly.GetExecutingAssembly().GetName().Version + "  is patching methods...");
			
            LoadFieldReplacements();
			AddAdditionalReplacements();
			ApplyFieldReplacements();
			PatchDestructiveFixes();
			PatchNonDestructiveFixes();
			PatchModCompatibility();
			Log.Message("RimThreaded patching is complete.");
		}

        private static void AddAdditionalReplacements()
        {
			SimplePool_Patch_RunNonDestructivePatches();
			Dijkstra_Patch_RunNonDestructivePatches();
			replaceFields.Add(Field(typeof(Region), "closedIndex"), Method(typeof(RegionTraverser_Transpile), "GetRegionClosedIndex"));
			replaceFields.Add(Method(typeof(Time), "get_realtimeSinceStartup"), Method(typeof(Time_Patch), "get_realtimeSinceStartup"));
#if DEBUG
			Material_Patch.RunDestructivePatches();
			Transform_Patch.RunDestructivePatches();
			UnityEngine_Object_Patch.RunDestructivePatches();
			replaceFields.Add(Method(typeof(Time), "get_frameCount"), Method(typeof(Time_Patch), "get_frameCount"));
			replaceFields.Add(Method(typeof(Time), "get_time"), Method(typeof(Time_Patch), "get_time"));
			replaceFields.Add(Method(typeof(Component), "get_transform"), Method(typeof(Component_Patch), "get_transform"));
			replaceFields.Add(Method(typeof(GameObject), "get_transform"), Method(typeof(GameObject_Patch), "get_transform"));
#endif
		}
#pragma warning disable 649
		[Serializable]
		class Replacements
		{
			public List<ClassReplacement> ClassReplacements;
		}

		[Serializable]
		class ClassReplacement
		{
			public string ClassName;
			public List<ThreadStaticDetail> ThreadStatics;
		}
		[Serializable]
		class ThreadStaticDetail
		{
			public string FieldName;
			public string PatchedClassName;
			public bool SelfInitialized;
		}
#pragma warning restore 649

		static Replacements replacements;
		private static void LoadFieldReplacements()
		{
			string fileName = "replacements3.json";
            string jsonString = File.ReadAllText(fileName);
            replacements = JsonConvert.DeserializeObject<Replacements>(jsonString);

            //IEnumerable<Assembly> source = from a in AppDomain.CurrentDomain.GetAssemblies()
            //                               where !a.FullName.StartsWith("Microsoft.VisualStudio")
            //                               select a;
            //foreach (Assembly a in source)
            //{
            //    Type[] b = GetTypesFromAssembly(a);
            //    if (a.ManifestModule.Name.Equals("Assembly-CSharp.dll"))
            //    {
            //        foreach (Type c in b)
            //        {
            //            if (c.FullName.Contains("BFSW"))
            //            {
            //                Log.Message(c.FullName);
            //                //Console.WriteLine(c.FullName);
            //            }
            //        }
            //    }
            //}
            MethodInfo initializer = Method(typeof(RimThreaded), "InitializeAllThreadStatics"); 
			ConstructorInfo threadStaticConstructor = typeof(ThreadStaticAttribute).GetConstructor(new Type[0]);
			CustomAttributeBuilder attributeBuilder = new CustomAttributeBuilder(threadStaticConstructor, new object[0]);
			foreach (ClassReplacement classReplacement in replacements.ClassReplacements)
            {

				Type type = TypeByName(classReplacement.ClassName);
				if(type == null)
                {
					Log.Error("Cannot find class named: " + classReplacement.ClassName);
					continue;
                }
				if (classReplacement.ThreadStatics != null && classReplacement.ThreadStatics.Count > 0)
				{
					AssemblyName aName = new AssemblyName(type.Name + "_Replacement");
					AssemblyBuilder ab = AppDomain.CurrentDomain.DefineDynamicAssembly(aName, AssemblyBuilderAccess.RunAndSave);
					ModuleBuilder modBuilder = ab.DefineDynamicModule(aName.Name, aName.Name + ".dll");
					TypeBuilder tb = modBuilder.DefineType(type.Name + "_Replacement", TypeAttributes.Public);
					MethodBuilder mb = tb.DefineMethod("InitializeThreadStatics", MethodAttributes.Public | MethodAttributes.Static, typeof(void), Type.EmptyTypes);
					//mb.InitLocals = true;
					ILGenerator il = mb.GetILGenerator();
					foreach (ThreadStaticDetail threadStaticDetail in classReplacement.ThreadStatics)
					{
						FieldInfo fieldInfo = Field(type, threadStaticDetail.FieldName);
                        if (fieldInfo == null)
                        {
                            Log.Error("Cannot find field named: " + classReplacement.ClassName + "." + threadStaticDetail.FieldName);
                            continue;
                        }
                        FieldInfo replacementField;
                        if (threadStaticDetail.PatchedClassName == null)
                        {							
							FieldBuilder fb = tb.DefineField(fieldInfo.Name, fieldInfo.FieldType, FieldAttributes.Public | FieldAttributes.Static);
							fb.SetCustomAttribute(attributeBuilder);
							replacementField = fb;
						}
						else
                        {
                            Type replacementType = TypeByName(threadStaticDetail.PatchedClassName);
                            if (replacementType == null)
                            {
                                Log.Error("Cannot find replacement class named: " + threadStaticDetail.PatchedClassName);
                                continue;
                            }
                            replacementField = Field(replacementType, threadStaticDetail.FieldName);
                            if (replacementField == null)
                            {
                                Log.Error("Cannot find replacement field named: " + threadStaticDetail.PatchedClassName + "." + threadStaticDetail.FieldName);
                                continue;
                            }
                        }
                        replaceFields[fieldInfo] = replacementField;
						if (!threadStaticDetail.SelfInitialized)
						{
							ConstructorInfo constructor = fieldInfo.FieldType.GetConstructor(Type.EmptyTypes);
							if (constructor != null)
							{
								il.Emit(OpCodes.Newobj, constructor);
								il.Emit(OpCodes.Stsfld, replacementField);
							}
						}
					}
					il.Emit(OpCodes.Ret);
					Type newFieldType = tb.CreateType();
					//Directory.SetCurrentDirectory("C:\\STUFF");
#if DEBUG
					ab.Save(type.Name + "_Replacement.dll");
#endif
					MethodInfo mb2 = Method(newFieldType, "InitializeThreadStatics");
					HarmonyMethod pf = new HarmonyMethod(mb2);
					harmony.Patch(initializer, postfix: pf);
				}
            }
        }
		public static Dictionary<Type, HashSet<FieldInfo>> untouchedStaticFields = new Dictionary<Type, HashSet<FieldInfo>>();
		public static HashSet<string> fieldFullNames = new HashSet<string>();
		public static HashSet<string> allStaticFieldNames = new HashSet<string>();
		private static void ApplyFieldReplacements()
        {
			IEnumerable<Assembly> source = from a in AppDomain.CurrentDomain.GetAssemblies()
                                           where !a.FullName.StartsWith("Microsoft.VisualStudio")
                                           select a;
            foreach (Assembly a in source)
            {
                Type[] types = GetTypesFromAssembly(a);
                if (a.ManifestModule.Name.Equals("Assembly-CSharp.dll"))
                {
                    foreach (Type type in types)
                    {
						//foreach (FieldInfo field in type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static))
      //                  {
						//	if((field.Name.StartsWith("tmp") || field.Name.StartsWith("temp")) && !replaceFields.ContainsKey(field))
      //                      {
						//		if(!untouchedStaticFields.TryGetValue(type, out HashSet<FieldInfo> fields)) {
						//			fields = new HashSet<FieldInfo>();
						//			untouchedStaticFields[type] = fields;
						//		}
						//		fields.Add(field);
						//		fieldFullNames.Add(type.Name + "." + field.Name);
						//		bool classExists = false;
						//		int count = 0;
						//		foreach (ClassReplacement classReplacement in replacements.ClassReplacements)
      //                          {
						//			count++;
						//			if(classReplacement.ClassName.Equals(type.FullName))
      //                              {
						//				classExists = true;
						//				bool fieldExists = false;
						//				foreach(ThreadStaticDetail threadStaticDetail in classReplacement.ThreadStatics)
      //                                  {
						//					if (threadStaticDetail.FieldName.Equals(field.Name))
						//					{
						//						fieldExists = true;
						//						break;
						//					}
      //                                  }
						//				if (!fieldExists)
						//				{
						//					classReplacement.ThreadStatics.Add(new ThreadStaticDetail()
						//					{
						//						FieldName = field.Name
						//					}
						//					);
						//				}
						//				break;
						//			}
      //                          }
						//		if(!classExists)
      //                          {
						//			replacements.ClassReplacements.Add(new ClassReplacement()
						//			{
						//				ClassName = type.FullName,
						//				ThreadStatics = new List<ThreadStaticDetail>() {
						//					new ThreadStaticDetail()
						//					{
						//						FieldName = field.Name
						//					}
						//				}
						//			});
						//		}

						//	}

						//}
                        foreach (MethodInfo method in type.GetMethods())
                        {
							if (method.IsDeclaredMember())
							{
								try {
									IEnumerable<KeyValuePair<OpCode, object>> f = PatchProcessor.ReadMethodBody(method);
									foreach (KeyValuePair<OpCode, object> e in f)
									{
										if (e.Value is FieldInfo fieldInfo && replaceFields.ContainsKey(fieldInfo))
										{
											TranspileFieldReplacements(method);
											break;
										}
										if (e.Value is MethodInfo methodInfo && replaceFields.ContainsKey(methodInfo))
										{
											TranspileFieldReplacements(method);
											break;
										}
									}
								} catch(NotSupportedException) {}
							}
                        }
                    }
                }
            }
			//JsonSerializer serializer = new JsonSerializer();
			//serializer.NullValueHandling = NullValueHandling.Ignore;

			//using (StreamWriter sw = new StreamWriter(@"replacements4.json"))
			//using (JsonWriter writer = new JsonTextWriter(sw))
			//{
			//	serializer.Serialize(writer, replacements);
			//}
			Log.Message("RimThreaded Field Replacements Complete. Initializing all ThreadStatics...");
			RimThreaded.InitializeAllThreadStatics();
   //         List<CodeInstruction> g = PatchProcessor.GetCurrentInstructions(Method(TypeByName("RimWorld.Pawn_MeleeVerbs"), "PawnMeleeVerbsStaticUpdate"));
			//foreach(CodeInstruction h in g)
   //         {
			//	Log.Message(h.ToString());
   //         }
		}



		public static List<CodeInstruction> EnterLock(LocalBuilder lockObject, LocalBuilder lockTaken, List<CodeInstruction> loadLockObjectInstructions, CodeInstruction currentInstruction)
		{
			List<CodeInstruction> codeInstructions = new List<CodeInstruction>();
			loadLockObjectInstructions[0].labels = currentInstruction.labels;
			for (int i = 0; i < loadLockObjectInstructions.Count; i++)
			{
				codeInstructions.Add(loadLockObjectInstructions[i]);
			}
			currentInstruction.labels = new List<Label>();
			codeInstructions.Add(new CodeInstruction(OpCodes.Stloc, lockObject.LocalIndex));
			codeInstructions.Add(new CodeInstruction(OpCodes.Ldc_I4_0));
			codeInstructions.Add(new CodeInstruction(OpCodes.Stloc, lockTaken.LocalIndex));
			CodeInstruction codeInstruction = new CodeInstruction(OpCodes.Ldloc, lockObject.LocalIndex);
			codeInstruction.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));
			codeInstructions.Add(codeInstruction);
			codeInstructions.Add(new CodeInstruction(OpCodes.Ldloca_S, lockTaken.LocalIndex));
			codeInstructions.Add(new CodeInstruction(OpCodes.Call, Method(typeof(Monitor), "Enter",
				new Type[] { typeof(object), typeof(bool).MakeByRefType() })));
			return codeInstructions;
		}
		public static List<CodeInstruction> ExitLock(ILGenerator iLGenerator, LocalBuilder lockObject, LocalBuilder lockTaken, CodeInstruction currentInstruction)
		{
			List<CodeInstruction> codeInstructions = new List<CodeInstruction>();
			Label endHandlerDestination = iLGenerator.DefineLabel();
			codeInstructions.Add(new CodeInstruction(OpCodes.Leave_S, endHandlerDestination));
			CodeInstruction codeInstruction = new CodeInstruction(OpCodes.Ldloc, lockTaken.LocalIndex);
			codeInstruction.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginFinallyBlock));
			codeInstructions.Add(codeInstruction);
			Label endFinallyDestination = iLGenerator.DefineLabel();
			codeInstructions.Add(new CodeInstruction(OpCodes.Brfalse_S, endFinallyDestination));
			codeInstructions.Add(new CodeInstruction(OpCodes.Ldloc, lockObject.LocalIndex));
			codeInstructions.Add(new CodeInstruction(OpCodes.Call, Method(typeof(Monitor), "Exit")));
			codeInstruction = new CodeInstruction(OpCodes.Endfinally);
			codeInstruction.labels.Add(endFinallyDestination);
			codeInstruction.blocks.Add(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
			codeInstructions.Add(codeInstruction);
			currentInstruction.labels.Add(endHandlerDestination);
			return codeInstructions;
		}

		public static List<CodeInstruction> GetLockCodeInstructions(
			ILGenerator iLGenerator, List<CodeInstruction> instructionsList, int currentInstructionIndex,
			int searchInstructionsCount, List<CodeInstruction> loadLockObjectInstructions,
			LocalBuilder lockObject, LocalBuilder lockTaken)
		{
			List<CodeInstruction> finalCodeInstructions = new List<CodeInstruction>();
			loadLockObjectInstructions[0].labels = instructionsList[currentInstructionIndex].labels;
			for (int i = 0; i < loadLockObjectInstructions.Count; i++)
			{
				finalCodeInstructions.Add(loadLockObjectInstructions[i]);
			}
			instructionsList[currentInstructionIndex].labels = new List<Label>();
			finalCodeInstructions.Add(new CodeInstruction(OpCodes.Stloc, lockObject.LocalIndex));
			finalCodeInstructions.Add(new CodeInstruction(OpCodes.Ldc_I4_0));
			finalCodeInstructions.Add(new CodeInstruction(OpCodes.Stloc, lockTaken.LocalIndex));
			CodeInstruction codeInstruction = new CodeInstruction(OpCodes.Ldloc, lockObject.LocalIndex);
			codeInstruction.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));
			finalCodeInstructions.Add(codeInstruction);
			finalCodeInstructions.Add(new CodeInstruction(OpCodes.Ldloca_S, lockTaken.LocalIndex));
			finalCodeInstructions.Add(new CodeInstruction(OpCodes.Call, Method(typeof(Monitor), "Enter",
				new Type[] { typeof(object), typeof(bool).MakeByRefType() })));
			for (int i = 0; i < searchInstructionsCount; i++)
			{
				finalCodeInstructions.Add(instructionsList[currentInstructionIndex]);
				currentInstructionIndex++;
			}
			Label endHandlerDestination = iLGenerator.DefineLabel();
			finalCodeInstructions.Add(new CodeInstruction(OpCodes.Leave_S, endHandlerDestination));
			codeInstruction = new CodeInstruction(OpCodes.Ldloc, lockTaken.LocalIndex);
			codeInstruction.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginFinallyBlock));
			finalCodeInstructions.Add(codeInstruction);
			Label endFinallyDestination = iLGenerator.DefineLabel();
			finalCodeInstructions.Add(new CodeInstruction(OpCodes.Brfalse_S, endFinallyDestination));
			finalCodeInstructions.Add(new CodeInstruction(OpCodes.Ldloc, lockObject.LocalIndex));
			finalCodeInstructions.Add(new CodeInstruction(OpCodes.Call, Method(typeof(Monitor), "Exit")));
			codeInstruction = new CodeInstruction(OpCodes.Endfinally);
			codeInstruction.labels.Add(endFinallyDestination);
			codeInstruction.blocks.Add(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
			finalCodeInstructions.Add(codeInstruction);
			instructionsList[currentInstructionIndex].labels.Add(endHandlerDestination);
			return finalCodeInstructions;
		}
		public static List<CodeInstruction> GetLockCodeInstructions(
			ILGenerator iLGenerator, List<CodeInstruction> instructionsList, int currentInstructionIndex,
			int searchInstructionsCount, List<CodeInstruction> loadLockObjectInstructions, Type lockObjectType)
		{
			LocalBuilder lockObject = iLGenerator.DeclareLocal(lockObjectType);
			LocalBuilder lockTaken = iLGenerator.DeclareLocal(typeof(bool));
			return GetLockCodeInstructions(iLGenerator, instructionsList, currentInstructionIndex,
				searchInstructionsCount, loadLockObjectInstructions, lockObject, lockTaken);
		}

		public static bool IsCodeInstructionsMatching(List<CodeInstruction> searchInstructions, List<CodeInstruction> instructionsList, int instructionIndex)
		{
			bool instructionsMatch = false;
			if (instructionIndex + searchInstructions.Count < instructionsList.Count)
			{
				instructionsMatch = true;
				for (int searchIndex = 0; searchIndex < searchInstructions.Count; searchIndex++)
				{
					CodeInstruction searchInstruction = searchInstructions[searchIndex];
					CodeInstruction originalInstruction = instructionsList[instructionIndex + searchIndex];
					object searchOperand = searchInstruction.operand;
					object orginalOperand = originalInstruction.operand;
					if (searchInstruction.opcode != originalInstruction.opcode)
					{
						instructionsMatch = false;
						break;
					}
					else
					{
						if (orginalOperand != null &&
							searchOperand != null &&
							orginalOperand != searchOperand)
						{
							if (orginalOperand.GetType() != typeof(LocalBuilder))
							{
								instructionsMatch = false;
								break;
							}
							else
							{
								if (((LocalBuilder)orginalOperand).LocalIndex != (int)searchOperand)
								{
									instructionsMatch = false;
									break;
								}
							}
						}
					}
				}
			}
			return instructionsMatch;
		}
		public static void AddBreakDestination(List<CodeInstruction> instructionsList, int currentInstructionIndex, Label breakDestination)
		{
			//Since we are going to break inside of some kind of loop, we need to find out where to jump/break to
			//The destination should be one line after the closing bracket of the loop when the exception/break occurs			
			HashSet<Label> labels = new HashSet<Label>();

			//gather all labels that exist at or above currentInstructionIndex. the start of our loop is going to be one of these...
			for (int i = 0; i <= currentInstructionIndex; i++)
			{
				foreach (Label label in instructionsList[i].labels)
				{
					labels.Add(label);
				}
			}

			//find first branch that jumps to label above currentInstructionIndex. the first branch opcode found is likely the closing bracket
			for (int i = currentInstructionIndex + 1; i < instructionsList.Count; i++)
			{
				if (instructionsList[i].operand is Label label)
				{
					if (labels.Contains(label))
					{
						instructionsList[i + 1].labels.Add(breakDestination);
						break;
					}
				}
			}
		}

		public static void StartTryAndAddBreakDestinationLabel(List<CodeInstruction> instructionsList, ref int currentInstructionIndex, Label breakDestination)
        {
			AddBreakDestination(instructionsList, currentInstructionIndex, breakDestination);
			instructionsList[currentInstructionIndex].blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));
		}

		public static List<CodeInstruction> EndTryStartCatchArgumentExceptionOutOfRange(List<CodeInstruction> instructionsList, ref int currentInstructionIndex, ILGenerator iLGenerator, Label breakDestination)
		{
			List<CodeInstruction> codeInstructions = new List<CodeInstruction>();
			Label handlerEnd = iLGenerator.DefineLabel();
			codeInstructions.Add(new CodeInstruction(OpCodes.Leave_S, handlerEnd));
			CodeInstruction pop = new CodeInstruction(OpCodes.Pop);
			pop.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginCatchBlock, typeof(ArgumentOutOfRangeException)));
			codeInstructions.Add(pop);
			CodeInstruction leaveLoopEnd = new CodeInstruction(OpCodes.Leave, breakDestination);
			leaveLoopEnd.blocks.Add(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
			codeInstructions.Add(leaveLoopEnd);
			instructionsList[currentInstructionIndex].labels.Add(handlerEnd);
			return codeInstructions;
		}

		public static List<CodeInstruction> UpdateTryCatchCodeInstructions(ILGenerator iLGenerator,
			List<CodeInstruction> instructionsList, int currentInstructionIndex, int searchInstructionsCount)
		{
			Label breakDestination = iLGenerator.DefineLabel();
			AddBreakDestination(instructionsList, currentInstructionIndex, breakDestination);
			List<CodeInstruction> finalCodeInstructions = new List<CodeInstruction>();
			instructionsList[currentInstructionIndex].blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock));
			for (int i = 0; i < searchInstructionsCount; i++)
			{
				finalCodeInstructions.Add(instructionsList[currentInstructionIndex]);
				currentInstructionIndex++;
			}
			Label handlerEnd = iLGenerator.DefineLabel();
			finalCodeInstructions.Add(new CodeInstruction(OpCodes.Leave_S, handlerEnd));
			CodeInstruction pop = new CodeInstruction(OpCodes.Pop);
			pop.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginCatchBlock, typeof(ArgumentOutOfRangeException)));
			finalCodeInstructions.Add(pop);
			CodeInstruction leaveLoopEnd = new CodeInstruction(OpCodes.Leave, breakDestination);
			leaveLoopEnd.blocks.Add(new ExceptionBlock(ExceptionBlockType.EndExceptionBlock));
			finalCodeInstructions.Add(leaveLoopEnd);
			instructionsList[currentInstructionIndex].labels.Add(handlerEnd);
			return finalCodeInstructions;
		}

		public static IEnumerable<CodeInstruction> WrapMethodInInstanceLock(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
		{
			List<CodeInstruction> instructionsList = instructions.ToList();
			int i = 0;
			List<CodeInstruction> loadLockObjectInstructions = new List<CodeInstruction>
			{
				new CodeInstruction(OpCodes.Ldarg_0)
			};
			LocalBuilder lockObject = iLGenerator.DeclareLocal(typeof(object));
			LocalBuilder lockTaken = iLGenerator.DeclareLocal(typeof(bool));
			foreach (CodeInstruction ci in EnterLock(lockObject, lockTaken, loadLockObjectInstructions, instructionsList[i]))
				yield return ci;
			while (i < instructionsList.Count - 1)
			{
				yield return instructionsList[i++];
			}
			foreach (CodeInstruction ci in ExitLock(iLGenerator, lockObject, lockTaken, instructionsList[i]))
				yield return ci;
			yield return instructionsList[i++];
		}

		//public static readonly Dictionary<Type, Type> threadStaticPatches = new Dictionary<Type, Type>();
		public static readonly Dictionary<object, object> replaceFields = new Dictionary<object, object>();

		//public static void AddAllMatchingFields(Type original, Type patched, bool matchStaticFieldsOnly = true)
		//{
		//	//IEnumerable<KeyValuePair<FieldInfo, FieldInfo>> allMatchingFields = GetAllMatchingFields(original, patched, matchStaticFieldsOnly);
		//	//foreach (KeyValuePair<FieldInfo, FieldInfo> matchingFields in allMatchingFields)
		//	//{
		//	//	//Log.Message("Adding field replacement for: " + matchingFields.Key.DeclaringType.Name + "." + matchingFields.Key.Name + " with: " + matchingFields.Value.DeclaringType.Name + "." + matchingFields.Value.Name);
		//	//	replaceFields.Add(matchingFields.Key, matchingFields.Value);
		//	//}
		//}

		//public static IEnumerable<KeyValuePair<FieldInfo, FieldInfo>> GetAllMatchingFields(Type original, Type patched, bool matchStaticFieldsOnly = true)
		//{
		//	foreach (FieldInfo newFieldInfo in GetDeclaredFields(patched))
		//	{
		//		if (!matchStaticFieldsOnly || newFieldInfo.IsStatic)
		//		{
		//			foreach (FieldInfo fieldInfo in GetDeclaredFields(original))
		//			{
		//				if (fieldInfo.Name.Equals(newFieldInfo.Name) && fieldInfo.FieldType == newFieldInfo.FieldType)
		//				{
		//					yield return new KeyValuePair<FieldInfo, FieldInfo>(fieldInfo, newFieldInfo);
		//				}
		//			}
		//		}
		//	}
		//}
		public static HashSet<object> notifiedObjects = new HashSet<object>();

		public static IEnumerable<CodeInstruction> ReplaceFieldsTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
		{
			notifiedObjects.Clear();
			foreach (CodeInstruction codeInstruction in instructions)
            {
                object operand = codeInstruction.operand;
                if (operand != null && replaceFields.TryGetValue(operand, out object newObjectInfo))
                {
                    switch (operand)
                    {
                        case FieldInfo fieldInfo:
                        {
                            switch (newObjectInfo)
                            {
                                case FieldInfo newFieldInfo:
#if DEBUG
									if (!notifiedObjects.Contains(newFieldInfo))
									{
										notifiedObjects.Add(newFieldInfo);
										Log.Message("RimThreaded is replacing field: " +
													fieldInfo.DeclaringType + "." + fieldInfo.Name +
													" with field: " + newFieldInfo.DeclaringType + "." + newFieldInfo.Name);
									}
#endif
                                    codeInstruction.operand = newFieldInfo;
                                    break;
                                case MethodInfo newMethodInfo:
#if DEBUG
										if (!notifiedObjects.Contains(newMethodInfo))
										{
											notifiedObjects.Add(newMethodInfo);
											Log.Message("RimThreaded is replacing field: " +
												fieldInfo.DeclaringType + "." + fieldInfo.Name +
												" with CALL method: " + newMethodInfo.DeclaringType + "." +
												newMethodInfo.Name);
										}
#endif
                                    codeInstruction.opcode = OpCodes.Call;
                                    codeInstruction.operand = newMethodInfo;
                                    break;
                            }

                            break;
                        }
                        case MethodInfo methodInfo:
                        {
                            switch (newObjectInfo)
                            {
                                case FieldInfo newFieldInfo:
#if DEBUG
									if (!notifiedObjects.Contains(newFieldInfo))
									{
										notifiedObjects.Add(newFieldInfo);
										Log.Message("RimThreaded is replacing method: " +
											methodInfo.DeclaringType + "." + methodInfo.Name +
											" with STATIC field: " + newFieldInfo.DeclaringType + "." +
											newFieldInfo.Name);
									}
#endif
                                    codeInstruction.opcode = OpCodes.Ldsfld;
                                    codeInstruction.operand = newFieldInfo;
                                    break;
                                case MethodInfo newMethodInfo:
#if DEBUG
									if (!notifiedObjects.Contains(newMethodInfo))
									{
										notifiedObjects.Add(newMethodInfo);
										Log.Message("RimThreaded is replacing method: " +
											methodInfo.DeclaringType + "." + methodInfo.Name +
											" with method: " + newMethodInfo.DeclaringType + "." +
											newMethodInfo.Name);
									}
#endif
                                    codeInstruction.operand = newMethodInfo;
                                    break;
                            }

                            break;
                        }
                    }
                }
                yield return codeInstruction;
			}
		}

		public static IEnumerable<CodeInstruction> Add3Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
		{
			List<CodeInstruction> instructionsList = instructions.ToList();
			int i = 0;
			while (i < instructionsList.Count)
			{
				if (i + 3 < instructionsList.Count && instructionsList[i + 3].opcode == OpCodes.Callvirt)
				{
					if (instructionsList[i + 3].operand is MethodInfo methodInfo)
					{
						if (methodInfo.Name.Equals("Add") && methodInfo.DeclaringType.FullName.Contains("System.Collections"))
						{
							LocalBuilder lockObject = iLGenerator.DeclareLocal(typeof(object));
							LocalBuilder lockTaken = iLGenerator.DeclareLocal(typeof(bool));
							List<CodeInstruction> loadLockObjectInstructions = new List<CodeInstruction>()
							{
								new CodeInstruction(OpCodes.Ldarg_0)
							};
							foreach (CodeInstruction lockInstruction in EnterLock(lockObject, lockTaken, loadLockObjectInstructions, instructionsList[i]))
							{
								yield return lockInstruction;
							}
							yield return instructionsList[i++];
							yield return instructionsList[i++];
							yield return instructionsList[i++];
							yield return instructionsList[i++];
							foreach (CodeInstruction lockInstruction in ExitLock(iLGenerator, lockObject, lockTaken, instructionsList[i]))
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


		public static readonly HarmonyMethod replaceFieldsHarmonyTranspiler = new HarmonyMethod(Method(typeof(RimThreadedHarmony), "ReplaceFieldsTranspiler"));
		public static readonly HarmonyMethod methodLockTranspiler = new HarmonyMethod(Method(typeof(RimThreadedHarmony), "WrapMethodInInstanceLock"));
        public static readonly HarmonyMethod add3Transpiler = new HarmonyMethod(Method(typeof(RimThreadedHarmony), "Add3Transpiler"));
		//public static readonly HarmonyMethod GameObjectTranspiler = new HarmonyMethod(Method(typeof(GameObject_Patch), "TranspileGameObjectConstructor"));
		//public static readonly HarmonyMethod InputGetMousePositionTranspiler = new HarmonyMethod(Method(typeof(UnityEngine_Input_Patch), "TranspileInputGetMousePosition"));
		//public static readonly HarmonyMethod TimeGetTimeTranspiler = new HarmonyMethod(Method(typeof(Time_Patch), "TranspileTimeGetTime"));
		//public static readonly HarmonyMethod TimeFrameCountTranspiler = new HarmonyMethod(Method(typeof(Time_Patch), "TranspileTimeFrameCount"));
		//public static readonly HarmonyMethod RealtimeSinceStartupTranspiler = new HarmonyMethod(Method(typeof(Time_Patch), "TranspileRealtimeSinceStartup"));
        //public static readonly HarmonyMethod ComponentTransformTranspiler = new HarmonyMethod(Method(typeof(Component_Patch), "TranspileComponentTransform"));
        //public static readonly HarmonyMethod GameObjectTransformTranspiler = new HarmonyMethod(Method(typeof(GameObject_Patch), "TranspileGameObjectTransform"));
		//public static readonly HarmonyMethod TransformPositionTranspiler = new HarmonyMethod(Method(typeof(Transform_Patch), "TranspileTransformPosition"));
        
		//public static void TranspileTimeFrameCountReplacement(Type original, string methodName, Type[] origType = null)
  //      {
  //          harmony.Patch(Method(original, methodName, origType), transpiler: TimeFrameCountTranspiler);
  //      }
		//public static void TranspileFieldReplacements(Type original, string methodName, Type[] origType = null)
		//{
		//	//Log.Message("RimThreaded is TranspilingFieldReplacements for method: " + original.Name + "." + methodName);
		//	//harmony.Patch(Method(original, methodName, origType), transpiler: replaceFieldsHarmonyTranspiler);
		//}
		public static void TranspileFieldReplacements(MethodBase original)
		{
			Log.Message("RimThreaded is TranspilingFieldReplacements for method: " + original.DeclaringType.FullName + "." + original.Name);
			harmony.Patch(original, transpiler: replaceFieldsHarmonyTranspiler);
		}

		public static void TranspileLockAdd3(Type original, string methodName, Type[] origType = null)
		{
			harmony.Patch(Method(original, methodName, origType), transpiler: add3Transpiler);
		}

		public static void Prefix(Type original, Type patched, string methodName, Type[] origType = null, bool destructive = true, int priority = 0)
		{
			MethodInfo oMethod = Method(original, methodName, origType);
			Type[] patch_type = null;
			if (origType != null)
			{
				patch_type = new Type[origType.Length];
				Array.Copy(origType, patch_type, origType.Length);

				if (!oMethod.ReturnType.Name.Equals("Void"))
				{
					Type[] temp_type = patch_type;
					patch_type = new Type[temp_type.Length + 1];
					patch_type[0] = oMethod.ReturnType.MakeByRefType();
					Array.Copy(temp_type, 0, patch_type, 1, temp_type.Length);
				}
				if (!oMethod.IsStatic)
				{
					Type[] temp_type = patch_type;
					patch_type = new Type[temp_type.Length + 1];
					patch_type[0] = original;
					Array.Copy(temp_type, 0, patch_type, 1, temp_type.Length);
				}
			}
			MethodInfo pMethod = Method(patched, methodName, patch_type);
			harmony.Patch(oMethod, prefix: new HarmonyMethod(pMethod, priority));
			if (!destructive)
			{
				nonDestructivePrefixes.Add(pMethod);
			}
		}

		public static void Postfix(Type original, Type patched, string originalMethodName, string patchedMethodName = null)
		{
			MethodInfo oMethod = Method(original, originalMethodName);
			if (patchedMethodName == null)
				patchedMethodName = originalMethodName;
			MethodInfo pMethod = Method(patched, patchedMethodName);
			harmony.Patch(oMethod, postfix: new HarmonyMethod(pMethod));
		}

		public static void Transpile(Type original, Type patched, string methodName, Type[] origType = null, string[] harmonyAfter = null)
		{
			MethodInfo oMethod = Method(original, methodName, origType);
			MethodInfo pMethod = Method(patched, methodName);
			HarmonyMethod transpilerMethod = new HarmonyMethod(pMethod)
			{
				after = harmonyAfter
			};
			try
			{
				harmony.Patch(oMethod, transpiler: transpilerMethod);
			} catch (Exception e)
            {
				Log.Error("Exception Transpiling: " + oMethod.ToString() + " " + transpilerMethod.ToString() + " " + e.ToString());
            }
		}
		public static void TranspileMethodLock(Type original, string methodName, Type[] origType = null, string[] harmonyAfter = null)
		{
			MethodInfo oMethod = Method(original, methodName, origType);
			harmony.Patch(oMethod, transpiler: methodLockTranspiler);
		}


		private static void PatchNonDestructiveFixes()
		{			
			GameObject_Patch.RunNonDestructivePatches(); //Prevents Game Crash from wooden floors and others

			//Simple
			AlertsReadout_Patch.RunNonDestructivesPatches(); //this will disable alert checks on ultrafast speed for an added speed boost
			Area_Patch.RunNonDestructivePatches(); //added for sowing area speedup
			Designator_Haul_Patch.RunNonDestructivePatches(); //add thing to hauling list when user specifically designates it via UI
            Designator_Unforbid_Patch.RunNonDestructivePatches(); //add thing to hauling list when user specifically unforbids it via UI
			TimeControls_Patch.RunNonDestructivePatches(); //TODO TRANSPILE - should releave needing TexButton2 class
			ZoneManager_Patch.RunNonDestructivePatches(); //recheck growing zone when upon zone grid add
			Zone_Patch.RunNonDestructivePatches(); //recheck growing zone when upon check haul destination call
			HediffGiver_Hypothermia_Transpile.RunNonDestructivePatches(); //speed up for comfy temperature
			LongEventHandler_Patch.RunNonDestructivePatches(); //replaced concurrentqueue and run init on new threads
			Map_Transpile.RunNonDestructivePatches(); //creates separate thread for skyManager.SkyManagerUpdate();
			Postfix(typeof(SlotGroup), typeof(HaulingCache), "Notify_AddedCell", "NewStockpileCreatedOrMadeUnfull"); //recheck growing zone when upon stockpile zone grid add

			//Complex
			//BattleLog_Transpile.RunNonDestructivePatches(); //if still causing issues, rewrite using ThreadSafeLinkedLists
			CompSpawnSubplant_Transpile.RunNonDestructivePatches();
			//GrammarResolver_Transpile.RunNonDestructivePatches();//reexamine complexity
			//GrammarResolverSimple_Transpile.RunNonDestructivePatches();//reexamine complexity
			HediffSet_Patch.RunNonDestructivePatches();
			PawnCapacitiesHandler_Patch.RunNonDestructivePatches(); //reexamine complexity?
			SituationalThoughtHandler_Patch.RunNonDestructivePatches();
			ThingOwnerThing_Transpile.RunNonDestructivePatches();
			TickList_Patch.RunNonDestructivePatches();
			WorkGiver_ConstructDeliverResources_Transpile.RunNonDestructivePatches(); //reexamine complexity
			WorkGiver_DoBill_Transpile.RunNonDestructivePatches(); //better way to find bills with cache
            Pawn_RelationsTracker_Patch.RunNonDestructivePatches(); 
		}

		private static void PatchDestructiveFixes()
		{
			Alert_MinorBreakRisk_Patch.RunDestructivePatches(); //performance rewrite
			AmbientSoundManager_Patch.RunDestructivePatches();
			AttackTargetsCache_Patch.RunDestructivesPatches(); //TODO: write ExposeData and change concurrentdictionary
			AudioSource_Patch.RunDestructivePatches();
			AudioSourceMaker_Patch.RunDestructivePatches();
			Building_Door_Patch.RunDestructivePatches(); //strange bug
			CompCauseGameCondition_Patch.RunDestructivePatches();
            CompSpawnSubplant_Transpile.RunDestructivePatches(); //could use interlock instead
			DateNotifier_Patch.RunDestructivePatches(); //performance boost when playing on only 1 map
            DesignationManager_Patch.RunDestructivePatches(); //added for development build
			DrugAIUtility_Patch.RunDestructivePatches();
			DynamicDrawManager_Patch.RunDestructivePatches();
			FactionManager_Patch.RunDestructivePatches();
			FilthMaker_Patch.RunDestructivePatches();
			GenClosest_Patch.RunDestructivePatches();
			GenCollection_Patch.RunDestructivePatches();
            GenSpawn_Patch.RunDestructivePatches();
			GenTemperature_Patch.RunDestructivePatches();
			GlobalControlsUtility_Patch.RunDestructivePatches();
			GrammarResolver_Patch.RunDestructivePatches();
			HediffGiver_Heat_Patch.RunDestructivePatches();
			HediffSet_Patch.RunDestructivePatches();
			ImmunityHandler_Patch.RunDestructivePatches();
			ListerThings_Patch.RunDestructivePatches();
			JobGiver_Work_Patch.RunDestructivePatches();
			JobMaker_Patch.RunDestructivePatches();
			LongEventHandler_Patch.RunDestructivePatches();
			Lord_Patch.RunDestructivePatches();
			LordManager_Patch.RunDestructivePatches();
			LordToil_Siege_Patch.RunDestructivePatches(); //TODO does locks around clears and adds. TRANSPILE
			Map_Patch.RunDestructivePatches(); //TODO - discover root cause
			MaterialPool_Patch.RunDestructivePatches();
			MemoryThoughtHandler_Patch.RunDestructivePatches();
			Pawn_HealthTracker_Patch.RunDestructivePatches(); //TODO re-add transpile
			Pawn_MindState_Patch.RunDestructivePatches(); //TODO - destructive hack for speed up - maybe not needed
			Pawn_PlayerSettings_Patch.RunDestructivePatches();
			Pawn_RelationsTracker_Patch.RunDestructivePatches();
			PawnPath_Patch.RunDestructivePatches();
			PawnUtility_Patch.RunDestructivePatches();
			PawnDestinationReservationManager_Patch.RunDestructivePatches();
			PlayLog_Patch.RunDestructivePatches();
			PortraitRenderer_Patch.RunDestructivePatches();
			PhysicalInteractionReservationManager_Patch.RunDestructivePatches(); //TODO: write ExposeData and change concurrent dictionary
			Rand_Patch.RunDestructivePatches(); //Simple
			Reachability_Patch.RunDestructivePatches();
			ReachabilityCache_Patch.RunDestructivePatches(); //TODO simplfy instance.fields
			RealtimeMoteList_Patch.RunDestructivePatches();
			RecipeWorkerCounter_Patch.RunDestructivePatches(); // rexamine purpose
			RegionAndRoomUpdater_Patch.RunDestructivePatches();
			RegionDirtyer_Patch.RunDestructivePatches();
			RegionGrid_Patch.RunDestructivePatches();
            RegionLink_Patch.RunDestructivePatches();
			RegionMaker_Patch.RunDestructivePatches();
			ResourceCounter_Patch.RunDestructivePatches();
			Sample_Patch.RunDestructivePatches();//TODO: low priority, reexamine sound
			SampleSustainer_Patch.RunDestructivePatches();//TODO: low priority, reexamine sound
			SeasonUtility_Patch.RunDestructivePatches(); //performance boost
			ShootLeanUtility_Patch.RunDestructivePatches(); //TODO: excessive locks, therefore RimThreadedHarmony.Prefix, conncurrent_queue could be transpiled in
			SoundSizeAggregator_Patch.RunDestructivePatches(); //TODO: low priority, reexamine sound
			SoundStarter_Patch.RunDestructivePatches(); //TODO: low priority, reexamine sound
			SteadyEnvironmentEffects_Patch.RunDestructivePatches();
			StoryState_Patch.RunDestructivePatches(); //WrapMethodInInstanceLock
			SubSustainer_Patch.RunDestructivePatches();//TODO: low priority, reexamine sound
			Sustainer_Patch.RunDestructivePatches();//TODO: low priority, reexamine sound
			SustainerAggregatorUtility_Patch.RunDestructivePatches();//TODO: low priority, reexamine sound
			SustainerManager_Patch.RunDestructivePatches();//TODO: low priority, reexamine sound
			TaleManager_Patch.RunDestructivePatches();
            Text_Patch.RunDestructivePatches();
			ThingGrid_Patch.RunDestructivePatches();
			ThinkNode_SubtreesByTag_Patch.RunDestructivePatches();
			TickManager_Patch.RunDestructivePatches();
			TileTemperaturesComp_Patch.RunDestructivePatches(); //TODO - good simple transpile candidate
			TradeShip_Patch.RunDestructivePatches();
			UniqueIDsManager_Patch.RunDestructivePatches();
			Verb_Patch.RunDestructivePatches(); // TODO: why is this causing null?
			WealthWatcher_Patch.RunDestructivePatches();
			WildPlantSpawner_Patch.RunDestructivePatches();
            WindManager_Patch.RunDestructivePatches();
            //WorkGiver_GrowerSow_Patch.RunDestructivePatches();
            WorldComponentUtility_Patch.RunDestructivePatches();
            WorldObjectsHolder_Patch.RunDestructivePatches();
            WorldPawns_Patch.RunDestructivePatches(); //todo examine GC optimization

            //complex methods that need further review for simplification
            AttackTargetReservationManager_Patch.RunDestructivePatches();
            BiomeDef_Patch.RunDestructivePatches();
            FloodFiller_Patch.RunDestructivePatches();//FloodFiller - inefficient global lock - threadstatics might help do these concurrently?
            JobQueue_Patch.RunDestructivePatches();
            MapPawns_Patch.RunDestructivePatches(); //TODO: Affects Animal Master Assignment
            MeditationFocusTypeAvailabilityCache_Patch.RunDestructivePatches();
            Pawn_JobTracker_Patch.RunDestructivePatches();
            Pawn_Patch.RunDestructivePatches();
            PawnCapacitiesHandler_Patch.RunDestructivePatches();
            Region_Patch.RunDestructivePatches();
            ReservationManager_Patch.RunDestructivePatches();
            Room_Patch.RunDestructivePatches();
            SituationalThoughtHandler_Patch.RunDestructivePatches();
            ThingOwnerUtility_Patch.RunDestructivePatches(); //TODO fix method reference by index

			//main-thread-only
			ContentFinder_Texture2D_Patch.RunDestructivePatches();
			GraphicDatabaseHeadRecords_Patch.RunDestructivePatches();
			Graphics_Patch.RunDestructivePatches();//Graphics (Giddy-Up and others)
			GUIStyle_Patch.RunDestructivePatches();
			LightningBoltMeshMaker_Patch.RunDestructivePatches();
			MapGenerator_Patch.RunDestructivePatches();//MapGenerator (Z-levels)
			MeshMakerPlanes_Patch.RunDestructivePatches();
			MeshMakerShadows_Patch.RunDestructivePatches();
			RenderTexture_Patch.RunDestructivePatches();//RenderTexture (Giddy-Up)
			SectionLayer_Patch.RunDestructivePatches();
			Texture2D_Patch.RunDestructivePatches();//Graphics (Giddy-Up)

			
		}

		private static void PatchModCompatibility()
		{
			AndroidTiers_Patch.Patch();
			AwesomeInventory_Patch.Patch();
			Children_Patch.Patch();
			CombatExteneded_Patch.Patch();
			Dubs_Skylight_Patch.Patch();
			GiddyUpCore_Patch.Patch();
			Hospitality_Patch.Patch();
			JobsOfOppurtunity_Patch.Patch();
			PawnRules_Patch.Patch();
			ZombieLand_Patch.Patch();
		}


		private static void PatchDevelopmentBuild()
		{
			//Development mode patches

			//TimeFrameCountTranspiler Fixes
			//harmony.Patch(Method(typeof(RimWorld.AlertsReadout), "AlertsReadoutUpdate"), transpiler: TimeFrameCountTranspiler);
			//harmony.Patch(Method(typeof(RimWorld.Alert_Critical), "AlertActiveUpdate"), transpiler: TimeFrameCountTranspiler);
			//harmony.Patch(Method(typeof(RimWorld.Building_Bed), "ToggleForPrisonersByInterface"), transpiler: TimeFrameCountTranspiler);
			//harmony.Patch(Method(typeof(RimWorld.CompAbilityEffect_Chunkskip), "FindClosestChunks"), transpiler: TimeFrameCountTranspiler);
			//harmony.Patch(Method(typeof(RimWorld.GenWorld), "MouseTile"), transpiler: TimeFrameCountTranspiler);
			//harmony.Patch(Method(typeof(RimWorld.InfestationCellFinder), "DebugDraw"), transpiler: TimeFrameCountTranspiler);
			//harmony.Patch(Method(typeof(RimWorld.LessonAutoActivator), "LessonAutoActivatorUpdate"), transpiler: TimeFrameCountTranspiler);
			//harmony.Patch(Method(typeof(RimWorld.ListerHaulables), "DebugString"), transpiler: TimeFrameCountTranspiler);
			//harmony.Patch(Method(typeof(RimWorld.ListerMergeables), "DebugString"), transpiler: TimeFrameCountTranspiler);
			//harmony.Patch(Method(typeof(RimWorld.OverlayDrawHandler), "DrawPowerGridOverlayThisFrame"), transpiler: TimeFrameCountTranspiler);
			//harmony.Patch(Method(typeof(RimWorld.OverlayDrawHandler), "get_ShouldDrawPowerGrid"), transpiler: TimeFrameCountTranspiler);
			//harmony.Patch(Method(typeof(RimWorld.OverlayDrawHandler), "DrawZonesThisFrame"), transpiler: TimeFrameCountTranspiler);
			//harmony.Patch(Method(typeof(RimWorld.OverlayDrawHandler), "get_ShouldDrawZones"), transpiler: TimeFrameCountTranspiler);
			//harmony.Patch(Method(typeof(RimWorld.SocialCardUtility), "CheckRecache"), transpiler: TimeFrameCountTranspiler);
			//harmony.Patch(Method(typeof(RimWorld.Storyteller), "DebugString"), transpiler: TimeFrameCountTranspiler);
			//harmony.Patch(Method(typeof(Verse.Sound.MouseoverSounds), "SilenceForNextFrame"), transpiler: TimeFrameCountTranspiler);
			//harmony.Patch(Method(typeof(Verse.Sound.MouseoverSounds), "ResolveFrame"), transpiler: TimeFrameCountTranspiler);
			//harmony.Patch(Method(typeof(Verse.Sound.SubSustainer), "<.ctor>b__12_0"), transpiler: TimeFrameCountTranspiler);
			//harmony.Patch(Method(typeof(Verse.Sound.SubSustainer), "StartSample"), transpiler: TimeFrameCountTranspiler);
			//harmony.Patch(Method(typeof(Verse.Sound.Sustainer), "<.ctor>b__15_0"), transpiler: TimeFrameCountTranspiler);
			//harmony.Patch(Method(typeof(Verse.Sound.Sustainer), "SustainerUpdate"), transpiler: TimeFrameCountTranspiler);
			////harmony.Patch(Method(typeof(Verse.Sound.Sustainer), "Maintain"), transpiler: TimeFrameCountTranspiler);
			//harmony.Patch(Method(typeof(Verse.CameraDriver), "get_CurrentViewRect"), transpiler: TimeFrameCountTranspiler);
			//harmony.Patch(Method(typeof(Verse.CellRenderer), "InitFrame"), transpiler: TimeFrameCountTranspiler);
			//harmony.Patch(Method(typeof(Verse.DebugInputLogger), "InputLogOnGUI"), transpiler: TimeFrameCountTranspiler);
			//harmony.Patch(Method(typeof(Verse.DesignationDragger), "UpdateDragCellsIfNeeded"), transpiler: TimeFrameCountTranspiler);
			//harmony.Patch(Method(typeof(Verse.Dialog_Rename), "get_AcceptsInput"), transpiler: TimeFrameCountTranspiler);
			//harmony.Patch(Method(typeof(Verse.Dialog_Rename), "WasOpenedByHotkey"), transpiler: TimeFrameCountTranspiler);
			//harmony.Patch(Method(typeof(Verse.FloatMenuWorld), "DoWindowContents"), transpiler: TimeFrameCountTranspiler);
			//harmony.Patch(Method(typeof(Verse.GenUI), "GetWidthCached"), transpiler: TimeFrameCountTranspiler);
			//harmony.Patch(Method(typeof(Verse.GizmoGridDrawer), "get_HeightDrawnRecently"), transpiler: TimeFrameCountTranspiler);
			//harmony.Patch(Method(typeof(Verse.GizmoGridDrawer), "DrawGizmoGrid"), transpiler: TimeFrameCountTranspiler);
			//harmony.Patch(Method(typeof(Verse.GUIEventFilterForOSX), "CheckRejectGUIEvent"), transpiler: TimeFrameCountTranspiler);
			//harmony.Patch(Method(typeof(Verse.GUIEventFilterForOSX), "RejectEvent"), transpiler: TimeFrameCountTranspiler);
			//harmony.Patch(Method(typeof(Verse.RealTime), "Update"), transpiler: TimeFrameCountTranspiler);
			////harmony.Patch(Method(typeof(Verse.Region), "DangerFor"), transpiler: TimeFrameCountTranspiler);
			//harmony.Patch(Method(typeof(Verse.RoomGroupTempTracker), "DebugString"), transpiler: TimeFrameCountTranspiler);
			//harmony.Patch(Method(typeof(Verse.Root), "Update"), transpiler: TimeFrameCountTranspiler);
			//harmony.Patch(Method(typeof(Verse.ScreenshotTaker), "Update"), transpiler: TimeFrameCountTranspiler);
			//harmony.Patch(Method(typeof(Verse.ScreenshotTaker), "TakeNonSteamShot"), transpiler: TimeFrameCountTranspiler);
			//harmony.Patch(Method(typeof(Verse.UIHighlighter), "HighlightTag"), transpiler: TimeFrameCountTranspiler);
			//harmony.Patch(Method(typeof(Verse.UIHighlighter), "HighlightOpportunity"), transpiler: TimeFrameCountTranspiler);
			//harmony.Patch(Method(typeof(Verse.UIHighlighter), "UIHighlighterUpdate"), transpiler: TimeFrameCountTranspiler);
			//harmony.Patch(Method(typeof(Verse.UnityGUIBugsFixer), "FixDelta"), transpiler: TimeFrameCountTranspiler);

			//TimeGetTimeTranspiler Fixes
			//TODO add remaining methods
			//harmony.Patch(Method(typeof(LetterStack), "ReceiveLetter", new Type[] { typeof(Letter), typeof(string) }), transpiler: TimeGetTimeTranspiler);
			//harmony.Patch(Method(typeof(AlertBounce), "", new Type[] { typeof(), typeof(string) }), transpiler: TimeGetTimeTranspiler);
			//harmony.Patch(Method(typeof(Dialog_FormCaravan), "FlashMass", new Type[] { }), transpiler: TimeGetTimeTranspiler);

			//InputGetMousePositionTranspiler Fixes
			//TODO add remaining methods

			////RealtimeSinceStartupTranspiler
			//harmony.Patch(Method(typeof(RimWorld.Planet.WorldCameraDriver), "CalculateCurInputDollyVect"), transpiler: RealtimeSinceStartupTranspiler);
			//harmony.Patch(Method(typeof(RimWorld.Planet.WorldSelectionDrawer), "Notify_Selected"), transpiler: RealtimeSinceStartupTranspiler);
			//harmony.Patch(Method(typeof(RimWorld.Alert), "Notify_Started"), transpiler: RealtimeSinceStartupTranspiler);
			//harmony.Patch(Method(typeof(RimWorld.CompProjectileInterceptor), "GetCurrentAlpha_Idle"), transpiler: RealtimeSinceStartupTranspiler);
			//harmony.Patch(Method(typeof(RimWorld.CompProjectileInterceptor), "GetCurrentAlpha_Selected"), transpiler: RealtimeSinceStartupTranspiler);
			//harmony.Patch(Method(typeof(RimWorld.Designator_Place), "HandleRotationShortcuts"), transpiler: RealtimeSinceStartupTranspiler);
			//harmony.Patch(Method(typeof(RimWorld.LearningReadout), "LearningReadoutOnGUI"), transpiler: RealtimeSinceStartupTranspiler);
			//harmony.Patch(Method(typeof(RimWorld.Lesson), "get_AgeSeconds"), transpiler: RealtimeSinceStartupTranspiler);
			//harmony.Patch(Method(typeof(RimWorld.Lesson), "OnActivated"), transpiler: RealtimeSinceStartupTranspiler);
			//harmony.Patch(Method(typeof(RimWorld.OverlayDrawer), "RenderPulsingOverlayInternal"), transpiler: RealtimeSinceStartupTranspiler);
			//harmony.Patch(Method(typeof(RimWorld.PlaceWorker_WatermillGenerator), "DrawGhost"), transpiler: RealtimeSinceStartupTranspiler);
			//harmony.Patch(Method(typeof(RimWorld.Screen_Credits), "PreOpen"), transpiler: RealtimeSinceStartupTranspiler);
			//harmony.Patch(Method(typeof(RimWorld.Screen_Credits), "WindowUpdate"), transpiler: RealtimeSinceStartupTranspiler);
			//harmony.Patch(Method(typeof(RimWorld.SelectionDrawer), "Notify_Selected"), transpiler: RealtimeSinceStartupTranspiler);

			////harmony.Patch(Method(typeof(RimWorld.SelectionDrawerUtility), "CalculateSelectionBracketPositionsUI"), transpiler: RealtimeSinceStartupTranspiler);
			////harmony.Patch(Method(typeof(RimWorld.SelectionDrawerUtility), "CalculateSelectionBracketPositionsWorld"), transpiler: RealtimeSinceStartupTranspiler);
			//harmony.Patch(Method(typeof(Verse.Sound.Sample), "get_AgeRealTime"), transpiler: RealtimeSinceStartupTranspiler);
			//harmony.Patch(Constructor(typeof(Verse.Sound.Sample)), transpiler: RealtimeSinceStartupTranspiler);
			//harmony.Patch(Method(typeof(Verse.Sound.SampleSustainer), "get_Volume"), transpiler: RealtimeSinceStartupTranspiler);
			//harmony.Patch(Method(typeof(Verse.Sound.SoundParamSource_Perlin), "ValueFor"), transpiler: RealtimeSinceStartupTranspiler);
			//harmony.Patch(Method(typeof(Verse.Sound.SoundParamSource_SourceAge), "ValueFor"), transpiler: RealtimeSinceStartupTranspiler);
			//harmony.Patch(Method(typeof(Verse.Sound.SoundSlotManager), "CanPlayNow"), transpiler: RealtimeSinceStartupTranspiler);
			//harmony.Patch(Method(typeof(Verse.Sound.SoundSlotManager), "Notify_Played"), transpiler: RealtimeSinceStartupTranspiler);
			//harmony.Patch(Method(typeof(Verse.Sound.SubSustainer), "<.ctor>b__12_0"), transpiler: RealtimeSinceStartupTranspiler);
			//harmony.Patch(Method(typeof(Verse.Sound.SubSustainer), "StartSample"), transpiler: RealtimeSinceStartupTranspiler);
			////harmony.Patch(Method(typeof(Verse.Sound.SubSustainer), "SubSustainerUpdate"), transpiler: RealtimeSinceStartupTranspiler);
			//harmony.Patch(Method(typeof(Verse.Sound.Sustainer), "get_TimeSinceEnd"), transpiler: RealtimeSinceStartupTranspiler);
			//harmony.Patch(Method(typeof(Verse.Sound.Sustainer), "End"), transpiler: RealtimeSinceStartupTranspiler);
			//harmony.Patch(Method(typeof(Verse.ArenaUtility), "PerformBattleRoyale"), transpiler: RealtimeSinceStartupTranspiler);
			//harmony.Patch(Method(typeof(Verse.CameraDriver), "CalculateCurInputDollyVect"), transpiler: RealtimeSinceStartupTranspiler);
			//harmony.Patch(Method(typeof(Verse.CameraShaker), "get_ShakeOffset"), transpiler: RealtimeSinceStartupTranspiler);
			//harmony.Patch(Method(typeof(Verse.DesignationDragger), "DraggerUpdate"), transpiler: RealtimeSinceStartupTranspiler);
			//harmony.Patch(Method(typeof(Verse.Dialog_MessageBox), "get_TimeUntilInteractive"), transpiler: RealtimeSinceStartupTranspiler);
			//harmony.Patch(Method(typeof(Verse.Dialog_NodeTree), "get_InteractiveNow"), transpiler: RealtimeSinceStartupTranspiler);
			//harmony.Patch(Method(typeof(Verse.Dialog_ResolutionConfirm), "get_TimeUntilRevert"), transpiler: RealtimeSinceStartupTranspiler);
			//harmony.Patch(Constructor(typeof(Verse.Dialog_ResolutionConfirm), Type.EmptyTypes), transpiler: RealtimeSinceStartupTranspiler);
			//harmony.Patch(Method(typeof(Verse.GameInfo), "GameInfoOnGUI"), transpiler: RealtimeSinceStartupTranspiler);
			//harmony.Patch(Method(typeof(Verse.GameInfo), "GameInfoUpdate"), transpiler: RealtimeSinceStartupTranspiler);
			//harmony.Patch(Method(typeof(Verse.GameplayTipWindow), "DrawContents"), transpiler: RealtimeSinceStartupTranspiler);
			//harmony.Patch(Method(typeof(Verse.GenText), "MarchingEllipsis"), transpiler: RealtimeSinceStartupTranspiler);
			//harmony.Patch(Method(typeof(Verse.LongEventHandler), "UpdateCurrentEnumeratorEvent"), transpiler: RealtimeSinceStartupTranspiler);
			//harmony.Patch(Method(typeof(Verse.Mote), "get_AgeSecs"), transpiler: RealtimeSinceStartupTranspiler);
			//harmony.Patch(Method(typeof(Verse.Mote), "SpawnSetup"), transpiler: RealtimeSinceStartupTranspiler);
			//harmony.Patch(Method(typeof(Verse.Pulser), "PulseBrightness", new[] { typeof(float), typeof(float) }), transpiler: RealtimeSinceStartupTranspiler);
			//harmony.Patch(Method(typeof(Verse.RealTime), "Update"), transpiler: RealtimeSinceStartupTranspiler);
			//harmony.Patch(Method(typeof(Verse.Region), "DebugDrawMouseover"), transpiler: RealtimeSinceStartupTranspiler);
			//harmony.Patch(Method(typeof(Verse.ScreenFader), "get_CurTime"), transpiler: RealtimeSinceStartupTranspiler);
			//harmony.Patch(Method(typeof(Verse.TooltipHandler), "TipRegion", new[] { typeof(Rect), typeof(TipSignal) }), transpiler: RealtimeSinceStartupTranspiler);
			//harmony.Patch(Method(typeof(Verse.TooltipHandler), "DrawActiveTips"), transpiler: RealtimeSinceStartupTranspiler);
			//harmony.Patch(Method(typeof(Verse.Widgets), "CheckPlayDragSliderSound"), transpiler: RealtimeSinceStartupTranspiler);

			//TranspileComponentTransform Fixes
			//harmony.Patch(Method(typeof(RimWorld.Planet.DebugTile), "get_DistanceToCamera"), transpiler: ComponentTransformTranspiler);
			//harmony.Patch(Method(typeof(RimWorld.Planet.GenWorldUI), "CurUITileSize"), transpiler: ComponentTransformTranspiler);
			//harmony.Patch(Method(typeof(RimWorld.Planet.WorldCameraDriver), "get_CurrentRealPosition"), transpiler: ComponentTransformTranspiler);
			//harmony.Patch(Method(typeof(RimWorld.Planet.WorldCameraDriver), "Update"), transpiler: ComponentTransformTranspiler);
			//harmony.Patch(Method(typeof(RimWorld.Planet.WorldCameraDriver), "ApplyPositionToGameObject"), transpiler: ComponentTransformTranspiler);
			//harmony.Patch(Method(typeof(RimWorld.Planet.WorldCameraManager), "CreateWorldSkyboxCamera"), transpiler: ComponentTransformTranspiler);
			//harmony.Patch(Method(typeof(RimWorld.Planet.WorldFeatureTextMesh_Legacy), "get_Position"), transpiler: ComponentTransformTranspiler);
			//harmony.Patch(Method(typeof(RimWorld.Planet.WorldFeatureTextMesh_Legacy), "get_Rotation"), transpiler: ComponentTransformTranspiler);
			//harmony.Patch(Method(typeof(RimWorld.Planet.WorldFeatureTextMesh_Legacy), "set_Rotation"), transpiler: ComponentTransformTranspiler);
			//harmony.Patch(Method(typeof(RimWorld.Planet.WorldFeatureTextMesh_Legacy), "get_LocalPosition"), transpiler: ComponentTransformTranspiler);
			//harmony.Patch(Method(typeof(RimWorld.Planet.WorldFeatureTextMesh_Legacy), "set_LocalPosition"), transpiler: ComponentTransformTranspiler);
			//harmony.Patch(Method(typeof(RimWorld.Planet.WorldFeatureTextMesh_Legacy), "Init"), transpiler: ComponentTransformTranspiler);
			//harmony.Patch(Method(typeof(RimWorld.MusicManagerEntry), "StartPlaying"), transpiler: ComponentTransformTranspiler);
			//harmony.Patch(Method(typeof(RimWorld.MusicManagerPlay), "MusicUpdate"), transpiler: ComponentTransformTranspiler);
			//harmony.Patch(Method(typeof(RimWorld.Page_SelectStartingSite), "PostOpen"), transpiler: ComponentTransformTranspiler);
			//harmony.Patch(Method(typeof(RimWorld.PortraitCameraManager), "CreatePortraitCamera"), transpiler: ComponentTransformTranspiler);
			//harmony.Patch(Method(typeof(RimWorld.PortraitRenderer), "RenderPortrait"), transpiler: ComponentTransformTranspiler);
			//harmony.Patch(Constructor(typeof(Verse.Sound.AudioSourcePoolCamera)), transpiler: ComponentTransformTranspiler);
			//harmony.Patch(Method(typeof(Verse.Sound.Sample), "ToString"), transpiler: ComponentTransformTranspiler);
			//harmony.Patch(Method(typeof(Verse.Sound.SoundParamSource_CameraAltitude), "ValueFor"), transpiler: ComponentTransformTranspiler);
			//harmony.Patch(Method(typeof(Verse.CameraDriver), "get_CurrentRealPosition"), transpiler: ComponentTransformTranspiler);
			//harmony.Patch(Method(typeof(Verse.CameraDriver), "ApplyPositionToGameObject"), transpiler: ComponentTransformTranspiler);
			//harmony.Patch(Method(typeof(Verse.DamageWorker), "ExplosionStart"), transpiler: ComponentTransformTranspiler);
			//harmony.Patch(Method(typeof(Verse.SkyOverlay), "DrawOverlay"), transpiler: ComponentTransformTranspiler);
			//harmony.Patch(Method(typeof(Verse.SubcameraDriver), "Init"), transpiler: ComponentTransformTranspiler);


			//GameObjectTransformTranspiler
			//harmony.Patch(Method(typeof(MusicManagerEntry), "StartPlaying"), transpiler: GameObjectTransformTranspiler);
			//harmony.Patch(Method(typeof(MusicManagerPlay), "MusicUpdate"), transpiler: GameObjectTransformTranspiler);
			//harmony.Patch(Constructor(typeof(Verse.Sound.AudioSourcePoolCamera)), transpiler: GameObjectTransformTranspiler);
			//harmony.Patch(Constructor(typeof(AudioSourcePoolWorld)), transpiler: GameObjectTransformTranspiler);
			//harmony.Patch(Method(typeof(SampleOneShot), "TryMakeAndPlay"), transpiler: GameObjectTransformTranspiler);
			//harmony.Patch(Method(typeof(SampleSustainer), "TryMakeAndPlay"), transpiler: GameObjectTransformTranspiler);
			//harmony.Patch(Method(typeof(Sustainer), "get_CameraDistanceSquared"), transpiler: GameObjectTransformTranspiler);
			//harmony.Patch(Method(typeof(Sustainer), "UpdateRootObjectPosition"), transpiler: GameObjectTransformTranspiler);
			//harmony.Patch(Method(typeof(Sustainer), "Cleanup"), transpiler: GameObjectTransformTranspiler);
			//harmony.Patch(Method(typeof(CameraDriver), "ApplyPositionToGameObject"), transpiler: GameObjectTransformTranspiler);
			//harmony.Patch(Method(typeof(CameraSwooper), "OffsetCameraFrom"), transpiler: GameObjectTransformTranspiler);
			//harmony.Patch(Method(typeof(GenDebug), "DebugPlaceSphere"), transpiler: GameObjectTransformTranspiler);
			//harmony.Patch(Method(typeof(SubcameraDriver), "Init"), transpiler: GameObjectTransformTranspiler);


			/*
			//TransformPositionTranspiler
            harmony.Patch(Method(typeof(WorldCameraDriver), "ApplyPositionToGameObject"), transpiler: TransformPositionTranspiler);
            harmony.Patch(Method(typeof(PortraitCameraManager), "CreatePortraitCamera"), transpiler: TransformPositionTranspiler);
            harmony.Patch(Method(typeof(PortraitRenderer), "RenderPortrait"), transpiler: TransformPositionTranspiler);
            harmony.Patch(Constructor(typeof(AudioSourcePoolWorld)), transpiler: TransformPositionTranspiler);
            harmony.Patch(Method(typeof(SampleOneShot), "TryMakeAndPlay"), transpiler: TransformPositionTranspiler);
			*/
		}

		private static void SimplePool_Patch_RunNonDestructivePatches()
		{
			replaceFields.Add(Method(typeof(SimplePool<List<float>>), "Get"),
				Method(typeof(SimplePool_Patch<List<float>>), "Get"));
			replaceFields.Add(Method(typeof(SimplePool<List<float>>), "Return"),
				Method(typeof(SimplePool_Patch<List<float>>), "Return"));
			replaceFields.Add(Method(typeof(SimplePool<List<float>>), "get_FreeItemsCount"),
				Method(typeof(SimplePool_Patch<List<float>>), "get_FreeItemsCount"));

			replaceFields.Add(Method(typeof(SimplePool<List<Pawn>>), "Get"),
				Method(typeof(SimplePool_Patch<List<Pawn>>), "Get"));
			replaceFields.Add(Method(typeof(SimplePool<List<Pawn>>), "Return"),
				Method(typeof(SimplePool_Patch<List<Pawn>>), "Return"));
			replaceFields.Add(Method(typeof(SimplePool<List<Pawn>>), "get_FreeItemsCount"),
				Method(typeof(SimplePool_Patch<List<Pawn>>), "get_FreeItemsCount"));

			replaceFields.Add(Method(typeof(SimplePool<List<Sustainer>>), "Get"),
				Method(typeof(SimplePool_Patch<List<Sustainer>>), "Get"));
			replaceFields.Add(Method(typeof(SimplePool<List<Sustainer>>), "Return"),
				Method(typeof(SimplePool_Patch<List<Sustainer>>), "Return"));
			replaceFields.Add(Method(typeof(SimplePool<List<Sustainer>>), "get_FreeItemsCount"),
				Method(typeof(SimplePool_Patch<List<Sustainer>>), "get_FreeItemsCount"));

			replaceFields.Add(Method(typeof(SimplePool<List<IntVec3>>), "Get"),
				Method(typeof(SimplePool_Patch<List<IntVec3>>), "Get"));
			replaceFields.Add(Method(typeof(SimplePool<List<IntVec3>>), "Return"),
				Method(typeof(SimplePool_Patch<List<IntVec3>>), "Return"));
			replaceFields.Add(Method(typeof(SimplePool<List<IntVec3>>), "get_FreeItemsCount"),
				Method(typeof(SimplePool_Patch<List<IntVec3>>), "get_FreeItemsCount"));

			replaceFields.Add(Method(typeof(SimplePool<List<Thing>>), "Get"),
				Method(typeof(SimplePool_Patch<List<Thing>>), "Get"));
			replaceFields.Add(Method(typeof(SimplePool<List<Thing>>), "Return"),
				Method(typeof(SimplePool_Patch<List<Thing>>), "Return"));
			replaceFields.Add(Method(typeof(SimplePool<List<Thing>>), "get_FreeItemsCount"),
				Method(typeof(SimplePool_Patch<List<Thing>>), "get_FreeItemsCount"));

			replaceFields.Add(Method(typeof(SimplePool<List<Gizmo>>), "Get"),
				Method(typeof(SimplePool_Patch<List<Gizmo>>), "Get"));
			replaceFields.Add(Method(typeof(SimplePool<List<Gizmo>>), "Return"),
				Method(typeof(SimplePool_Patch<List<Gizmo>>), "Return"));
			replaceFields.Add(Method(typeof(SimplePool<List<Gizmo>>), "get_FreeItemsCount"),
				Method(typeof(SimplePool_Patch<List<Gizmo>>), "get_FreeItemsCount"));

			replaceFields.Add(Method(typeof(SimplePool<List<Hediff>>), "Get"),
				Method(typeof(SimplePool_Patch<List<Hediff>>), "Get"));
			replaceFields.Add(Method(typeof(SimplePool<List<Hediff>>), "Return"),
				Method(typeof(SimplePool_Patch<List<Hediff>>), "Return"));
			replaceFields.Add(Method(typeof(SimplePool<List<Hediff>>), "get_FreeItemsCount"),
				Method(typeof(SimplePool_Patch<List<Hediff>>), "get_FreeItemsCount"));

			replaceFields.Add(Method(typeof(SimplePool<HashSet<IntVec3>>), "Get"),
				Method(typeof(SimplePool_Patch<HashSet<IntVec3>>), "Get"));
			replaceFields.Add(Method(typeof(SimplePool<HashSet<IntVec3>>), "Return"),
				Method(typeof(SimplePool_Patch<HashSet<IntVec3>>), "Return"));
			replaceFields.Add(Method(typeof(SimplePool<HashSet<IntVec3>>), "get_FreeItemsCount"),
				Method(typeof(SimplePool_Patch<HashSet<IntVec3>>), "get_FreeItemsCount"));

			replaceFields.Add(Method(typeof(SimplePool<HashSet<Pawn>>), "Get"),
				Method(typeof(SimplePool_Patch<HashSet<Pawn>>), "Get"));
			replaceFields.Add(Method(typeof(SimplePool<HashSet<Pawn>>), "Return"),
				Method(typeof(SimplePool_Patch<HashSet<Pawn>>), "Return"));
			replaceFields.Add(Method(typeof(SimplePool<HashSet<Pawn>>), "get_FreeItemsCount"),
				Method(typeof(SimplePool_Patch<HashSet<Pawn>>), "get_FreeItemsCount"));

			replaceFields.Add(Method(typeof(SimplePool<Job>), "Get"),
				Method(typeof(SimplePool_Patch<Job>), "Get"));
			replaceFields.Add(Method(typeof(SimplePool<Job>), "Return"),
				Method(typeof(SimplePool_Patch<Job>), "Return"));
			replaceFields.Add(Method(typeof(SimplePool<Job>), "get_FreeItemsCount"),
				Method(typeof(SimplePool_Patch<Job>), "get_FreeItemsCount"));

		}

		private static void Dijkstra_Patch_RunNonDestructivePatches()
		{
			//replaceFields.Add(Method(typeof(Dijkstra<Region>), "Run", new Type[] {
			//	typeof(Region),
			//	typeof(Func<Region, IEnumerable<Region>>),
			//	typeof(Func<Region, Region, float>),
			//	typeof(List<KeyValuePair<Region, float>>),
			//	typeof(Dictionary<Region, Region>)
			//}),
			//	Method(typeof(Dijkstra_Patch<Region>), "Run", new Type[] {
			//	typeof(Region),
			//	typeof(Func<Region, IEnumerable<Region>>),
			//	typeof(Func<Region, Region, float>),
			//	typeof(List<KeyValuePair<Region, float>>),
			//	typeof(Dictionary<Region, Region>)
			//	}));

			replaceFields.Add(Method(typeof(Dijkstra<IntVec3>), "Run", new Type[] {
				typeof(IEnumerable<IntVec3>),
				typeof(Func<IntVec3, IEnumerable<IntVec3>>),
				typeof(Func<IntVec3, IntVec3, float>),
				typeof(List<KeyValuePair<IntVec3, float>>),
				typeof(Dictionary<IntVec3, IntVec3>)
			}),
				Method(typeof(Dijkstra_Patch<IntVec3>), "Run", new Type[] {
				typeof(IEnumerable<IntVec3>),
				typeof(Func<IntVec3, IEnumerable<IntVec3>>),
				typeof(Func<IntVec3, IntVec3, float>),
				typeof(List<KeyValuePair<IntVec3, float>>),
				typeof(Dictionary<IntVec3, IntVec3>) }
				));

			replaceFields.Add(Method(typeof(Dijkstra<IntVec3>), "Run", new Type[] {
				typeof(IntVec3),
				typeof(Func<IntVec3, IEnumerable<IntVec3>>),
				typeof(Func<IntVec3, IntVec3, float>),
				typeof(Dictionary<IntVec3, float>),
				typeof(Dictionary<IntVec3, IntVec3>)
			}),
				Method(typeof(Dijkstra_Patch<IntVec3>), "Run", new Type[] {
				typeof(IntVec3),
				typeof(Func<IntVec3, IEnumerable<IntVec3>>),
				typeof(Func<IntVec3, IntVec3, float>),
				typeof(Dictionary<IntVec3, float>),
				typeof(Dictionary<IntVec3, IntVec3>) }
				));

			replaceFields.Add(Method(typeof(Dijkstra<Region>), "Run", new Type[] {
				typeof(IEnumerable<Region>),
				typeof(Func<Region, IEnumerable<Region>>),
				typeof(Func<Region, Region, float>),
				typeof(Dictionary<Region, float>),
				typeof(Dictionary<Region, Region>)
			}),
				Method(typeof(Dijkstra_Patch<Region>), "Run", new Type[] {
				typeof(IEnumerable<Region>),
				typeof(Func<Region, IEnumerable<Region>>),
				typeof(Func<Region, Region, float>),
				typeof(Dictionary<Region, float>),
				typeof(Dictionary<Region, Region>) }
				));

			replaceFields.Add(Method(typeof(Dijkstra<int>), "Run", new Type[] {
				typeof(IEnumerable<int>),
				typeof(Func<int, IEnumerable<int>>),
				typeof(Func<int, int, float>),
				typeof(Dictionary<int, float>),
				typeof(Dictionary<int, int>)
			}),
				Method(typeof(Dijkstra_Patch<int>), "Run", new Type[] {
				typeof(IEnumerable<int>),
				typeof(Func<int, IEnumerable<int>>),
				typeof(Func<int, int, float>),
				typeof(Dictionary<int, float>),
				typeof(Dictionary<int, int>) }
				));

		}

	}

}
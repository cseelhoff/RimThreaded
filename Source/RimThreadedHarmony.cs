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
using static RimWorld.SituationalThoughtHandler;

namespace RimThreaded
{

	public class RimThreadedHarmony
	{
		public static Harmony harmony = new Harmony("majorhoff.rimthreaded");

		public static FieldInfo cachedStoreCell;
		public static HashSet<MethodInfo> nonDestructivePrefixes = new HashSet<MethodInfo>();
		public static List<Assembly> assemblies;

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

			string potentialConflicts = RimThreadedMod.getPotentialModConflicts();
			if (potentialConflicts.Length > 0)
			{
				Log.Warning("Potential RimThreaded mod conflicts :" + potentialConflicts);
			}
		}

        private static void AddAdditionalReplacements()
        {
			SimplePool_Patch_RunNonDestructivePatches();
			FullPool_Patch_RunNonDestructivePatches();
			Dijkstra_Patch_RunNonDestructivePatches();
			PathFinder_Patch.AddFieldReplacements();
			Region_Patch.AddFieldReplacements();
			replaceFields.Add(Method(typeof(Time), "get_realtimeSinceStartup"), Method(typeof(Time_Patch), "get_realtimeSinceStartup"));
#if DEBUG
			Material_Patch.RunDestructivePatches();
			Transform_Patch.RunDestructivePatches();
			UnityEngine_Object_Patch.RunDestructivePatches();
			replaceFields.Add(Method(typeof(Time), "get_frameCount"), Method(typeof(Time_Patch), "get_frameCount"));
			replaceFields.Add(Method(typeof(Time), "get_time"), Method(typeof(Time_Patch), "get_time"));
			replaceFields.Add(Method(typeof(Component), "get_transform"), Method(typeof(Component_Patch), "get_transform"));
			replaceFields.Add(Method(typeof(Component), "get_gameObject"), Method(typeof(Component_Patch), "get_gameObject"));
			replaceFields.Add(Method(typeof(GameObject), "get_transform"), Method(typeof(GameObject_Patch), "get_transform"));
			//replaceFields.Add(Method(typeof(GameObject), "GetComponent", Type.EmptyTypes), Method(typeof(GameObject_Patch), "GetComponent"));
			//replaceFields.Add(Method(typeof(GameObject), "GetComponent", Type.EmptyTypes, new Type[] { typeof(AudioReverbFilter) }),
			//	Method(typeof(GameObject_Patch), "GetComponent", new Type[] { typeof(GameObject) }, new Type[] { typeof(AudioReverbFilter) }));
			//replaceFields.Add(Method(typeof(GameObject), "GetComponent", Type.EmptyTypes, new Type[] { typeof(AudioLowPassFilter) }),
			//	Method(typeof(GameObject_Patch), "GetComponent", new Type[] { typeof(GameObject) }, new Type[] { typeof(AudioLowPassFilter) }));
			//replaceFields.Add(Method(typeof(GameObject), "GetComponent", Type.EmptyTypes, new Type[] { typeof(AudioHighPassFilter) }),
			//	Method(typeof(GameObject_Patch), "GetComponent", new Type[] { typeof(GameObject) }, new Type[] { typeof(AudioHighPassFilter) }));
			//replaceFields.Add(Method(typeof(GameObject), "GetComponent", Type.EmptyTypes, new Type[] { typeof(AudioEchoFilter) }),
			//	Method(typeof(GameObject_Patch), "GetComponent", new Type[] { typeof(GameObject) }, new Type[] { typeof(AudioEchoFilter) }));
			//replaceFields.Add(Method(typeof(GameObject), "AddComponent", Type.EmptyTypes, new Type[] { typeof(AudioReverbFilter) }),
			//	Method(typeof(GameObject_Patch), "AddComponent", new Type[] { typeof(GameObject) }, new Type[] { typeof(AudioReverbFilter) }));
			//replaceFields.Add(Method(typeof(GameObject), "AddComponent", Type.EmptyTypes, new Type[] { typeof(AudioLowPassFilter) }),
			//	Method(typeof(GameObject_Patch), "AddComponent", new Type[] { typeof(GameObject) }, new Type[] { typeof(AudioLowPassFilter) }));
			//replaceFields.Add(Method(typeof(GameObject), "AddComponent", Type.EmptyTypes, new Type[] { typeof(AudioHighPassFilter) }),
			//	Method(typeof(GameObject_Patch), "AddComponent", new Type[] { typeof(GameObject) }, new Type[] { typeof(AudioHighPassFilter) }));
			//replaceFields.Add(Method(typeof(GameObject), "AddComponent", Type.EmptyTypes, new Type[] { typeof(AudioEchoFilter) }),
			//	Method(typeof(GameObject_Patch), "AddComponent", new Type[] { typeof(GameObject) }, new Type[] { typeof(AudioEchoFilter) }));
			//replaceFields.Add(Method(typeof(Transform), "set_parent"), Method(typeof(Transform_Patch), "set_parent"));
			//replaceFields.Add(Method(typeof(Transform), "set_localPosition"), Method(typeof(Transform_Patch), "set_localPosition"));
			//replaceFields.Add(Method(typeof(AudioSource), "set_clip"), Method(typeof(AudioSource_Patch), "set_clip"));
			//replaceFields.Add(Method(typeof(AudioSource), "set_volume"), Method(typeof(AudioSource_Patch), "set_volume"));
			//replaceFields.Add(Method(typeof(AudioSource), "set_pitch"), Method(typeof(AudioSource_Patch), "set_pitch"));
			//replaceFields.Add(Method(typeof(AudioSource), "set_minDistance"), Method(typeof(AudioSource_Patch), "set_minDistance"));
			//replaceFields.Add(Method(typeof(AudioSource), "set_maxDistance"), Method(typeof(AudioSource_Patch), "set_maxDistance"));
			//replaceFields.Add(Method(typeof(AudioSource), "set_spatialBlend"), Method(typeof(AudioSource_Patch), "set_spatialBlend"));
			//replaceFields.Add(Method(typeof(AudioSource), "set_loop"), Method(typeof(AudioSource_Patch), "set_loop"));
			//replaceFields.Add(Method(typeof(AudioSource), "set_mute"), Method(typeof(AudioSource_Patch), "set_mute"));
			//replaceFields.Add(Method(typeof(AudioSource), "Play", Type.EmptyTypes), Method(typeof(AudioSource_Patch), "Play"));
			//replaceFields.Add(Method(typeof(AudioSource), "get_volume"), Method(typeof(AudioSource_Patch), "get_volume"));
#endif
			//replaceFields.Add(Method(typeof(AudioLowPassFilter), "set_cutoffFrequency"), Method(typeof(AudioLowPassFilter_Patch), "set_cutoffFrequency"));
			//replaceFields.Add(Method(typeof(AudioLowPassFilter), "set_lowpassResonanceQ"), Method(typeof(AudioLowPassFilter_Patch), "set_lowpassResonanceQ"));
			//replaceFields.Add(Method(typeof(AudioHighPassFilter), "set_cutoffFrequency"), Method(typeof(AudioHighPassFilter_Patch), "set_cutoffFrequency"));
			//replaceFields.Add(Method(typeof(AudioHighPassFilter), "set_highpassResonanceQ"), Method(typeof(AudioHighPassFilter_Patch), "set_highpassResonanceQ"));

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
			assemblies = (from a in AppDomain.CurrentDomain.GetAssemblies()
						  where !a.FullName.StartsWith("Microsoft.VisualStudio")
						  select a).ToList();
			//string replacementsJsonPath = Path.Combine(((Mod)RimThreadedMod).intContent.RootDir, "replacements.json"); 

			string jsonString = File.ReadAllText(RimThreadedMod.replacementsJsonPath);
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
			AssemblyName aName = new AssemblyName("RimThreadedReplacements");
			AssemblyBuilder ab = AppDomain.CurrentDomain.DefineDynamicAssembly(aName, AssemblyBuilderAccess.RunAndSave);
			ModuleBuilder modBuilder = ab.DefineDynamicModule(aName.Name, aName.Name + ".dll");
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
					TypeBuilder tb = modBuilder.DefineType(type.Name + "_Replacement", TypeAttributes.Public);
					MethodBuilder mb = tb.DefineMethod("InitializeThreadStatics", MethodAttributes.Public | MethodAttributes.Static, typeof(void), Type.EmptyTypes);
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
					MethodInfo mb2 = Method(newFieldType, "InitializeThreadStatics");
					HarmonyMethod pf = new HarmonyMethod(mb2);
					harmony.Patch(initializer, postfix: pf);
				}
            }
#if DEBUG
			ab.Save(aName.Name + ".dll");
#endif
		}
		public static Dictionary<Type, HashSet<FieldInfo>> untouchedStaticFields = new Dictionary<Type, HashSet<FieldInfo>>();
		public static HashSet<string> fieldFullNames = new HashSet<string>();
		public static HashSet<string> allStaticFieldNames = new HashSet<string>();
		//public static bool intializersReady = false;
		private static void ApplyFieldReplacements()
        {
            foreach (Assembly assembly in assemblies)
            {
                if (assembly.ManifestModule.Name.Equals("Assembly-CSharp.dll"))
				{
					Type[] types = GetTypesFromAssembly(assembly);
					foreach (Type type in types)
                    {
						List<MethodBase> allMethods = new List<MethodBase>();
						allMethods.AddRange(type.GetMethods(all | BindingFlags.DeclaredOnly));
						allMethods.AddRange(type.GetConstructors(all | BindingFlags.DeclaredOnly));

						foreach (MethodBase method in allMethods)
                        {
							if (method.IsDeclaredMember())
							{
								try {
									IEnumerable<KeyValuePair<OpCode, object>> codeInstructions = PatchProcessor.ReadMethodBody(method);
									foreach (KeyValuePair<OpCode, object> codeInstruction in codeInstructions)
									{
										if (codeInstruction.Value is FieldInfo fieldInfo && replaceFields.ContainsKey(fieldInfo))
										{
											TranspileFieldReplacements(method);
											break;
										}
										if (codeInstruction.Value is MethodInfo methodInfo && replaceFields.ContainsKey(methodInfo))
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

            //TranspileFieldReplacements(Method(typeof(SoundFilter), "GetOrMakeFilterOn",
            //	new Type[] { typeof(AudioSource) }, new Type[] { typeof(AudioReverbFilter) }));
            //TranspileFieldReplacements(Method(typeof(SoundFilter), "GetOrMakeFilterOn",
            //	new Type[] { typeof(AudioSource) }, new Type[] { typeof(AudioLowPassFilter) }));
            //TranspileFieldReplacements(Method(typeof(SoundFilter), "GetOrMakeFilterOn",
            //	new Type[] { typeof(AudioSource) }, new Type[] { typeof(AudioHighPassFilter) }));
            //TranspileFieldReplacements(Method(typeof(SoundFilter), "GetOrMakeFilterOn",
            //	new Type[] { typeof(AudioSource) }, new Type[] { typeof(AudioEchoFilter) }));
			Type pawnType = typeof(Pawn);
			Type thoughtsType = typeof(CachedSocialThoughts);
			string removeAllString = nameof(GenCollection.RemoveAll);
            MethodInfo removeAllGeneric = GetDeclaredMethods(typeof(GenCollection)).First(method => method.Name == removeAllString && method.GetGenericArguments().Count() == 2);
            MethodInfo removeAllPawnThoughts = removeAllGeneric.MakeGenericMethod(new[] { pawnType, thoughtsType });
			TranspileFieldReplacements(removeAllPawnThoughts);
			Log.Message("RimThreaded Field Replacements Complete.");
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

		public static readonly Dictionary<object, object> replaceFields = new Dictionary<object, object>();

		public static HashSet<object> notifiedObjects = new HashSet<object>();

		public static IEnumerable<CodeInstruction> ReplaceFieldsTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
		{
			notifiedObjects.Clear();
			foreach (CodeInstruction codeInstruction in instructions)
            {
#if DEBUG
				Log.messageCount = 0; //prevents logging to stop from spam
#endif
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
                                case Dictionary<OpCode, MethodInfo> newMethodInfoDict:
                                    MethodInfo newMethodInfo = newMethodInfoDict[codeInstruction.opcode];
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
			//---REQUIRED---
			GameObject_Patch.RunNonDestructivePatches(); //Prevents Game Crash from wooden floors and others
			LongEventHandler_Patch.RunNonDestructivePatches(); //replaced concurrentqueue and run init on new threads

			//---Simple---
			AlertsReadout_Patch.RunNonDestructivesPatches(); //this will disable alert checks on ultrafast speed for an added speed boost
            Area_Patch.RunNonDestructivePatches(); //added for sowing area speedup
			CompSpawnSubplant_Transpile.RunNonDestructivePatches(); //fixes bug with royalty spawning subplants
			Designator_Haul_Patch.RunNonDestructivePatches(); //add thing to hauling list when user specifically designates it via UI
            Designator_Unforbid_Patch.RunNonDestructivePatches(); //add thing to hauling list when user specifically unforbids it via UI
			PathFinder_Patch.RunNonDestructivePatches(); //simple initialize calcGrid on InitStatusesAndPushStartNode
			TimeControls_Patch.RunNonDestructivePatches(); //allow speed 4
            ZoneManager_Patch.RunNonDestructivePatches(); //recheck growing zone when upon zone grid add
            Zone_Patch.RunNonDestructivePatches(); //recheck growing zone when upon check haul destination call
            HediffGiver_Hypothermia_Transpile.RunNonDestructivePatches(); //speed up for comfy temperature
            Map_Transpile.RunNonDestructivePatches(); //creates separate thread for skyManager.SkyManagerUpdate();            
            //BattleLog_Transpile.RunNonDestructivePatches(); //if still causing issues, rewrite using ThreadSafeLinkedLists
            //GrammarResolver_Transpile.RunNonDestructivePatches();//reexamine complexity
            //GrammarResolverSimple_Transpile.RunNonDestructivePatches();//reexamine complexity
            HediffSet_Patch.RunNonDestructivePatches(); //TODO - replace 270 instances with ThreadSafeLinkedList
            ThingOwnerThing_Transpile.RunNonDestructivePatches(); //reexamine complexity?
			TickList_Patch.RunNonDestructivePatches(); //allows multithreaded calls of thing.tick longtick raretick
            //WorkGiver_ConstructDeliverResources_Transpile.RunNonDestructivePatches(); //reexamine complexity Jobs Of Oppurtunity?
            //WorkGiver_DoBill_Transpile.RunNonDestructivePatches(); //better way to find bills with cache
            //Pawn_RelationsTracker_Patch.RunNonDestructivePatches(); //transpile not needed with new threadsafe simplepools
			PawnCapacitiesHandler_Patch.RunNonDestructivePatches(); //TODO fix transpile for 1 of 2 methods try inside of try perhaps?
			Postfix(typeof(SlotGroup), typeof(HaulingCache), "Notify_AddedCell", "NewStockpileCreatedOrMadeUnfull"); //recheck growing zone when upon stockpile zone grid add

		}

		private static void PatchDestructiveFixes()
        {
			//---REQUIRED---
			TickManager_Patch.RunDestructivePatches(); //Redirects DoSingleTick to RimThreaded

			//---Main-thread-only calls--- TODO - low priority. These can likely be made more uniform
			AudioSource_Patch.RunDestructivePatches();
			AudioSourceMaker_Patch.RunDestructivePatches();
			ContentFinder_Texture2D_Patch.RunDestructivePatches();
			GenDraw_Patch.RunDestructivePatches(); // DrawMeshNowOrLater (HotSprings and Kijin Race)
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
			Text_Patch.RunDestructivePatches(); //unity get_CurFontStyle on main thread


			//---Multithreaded Ticking---
			FactionManager_Patch.RunDestructivePatches(); //allows multithreaded ticking of factions
			TradeShip_Patch.RunDestructivePatches(); //allows multithreaded ticking of tradeships
			WildPlantSpawner_Patch.RunDestructivePatches(); //allows multithreaded icking of WildPlantSpawner
			WindManager_Patch.RunDestructivePatches();//allows multithreaded icking of WindManager													  
			WorldComponentUtility_Patch.RunDestructivePatches(); //allows multithreaded icking of WorldComponentUtility
			WorldObjectsHolder_Patch.RunDestructivePatches(); //Class was largely overhauled to allow multithreaded ticking for WorldPawns.Tick()
			WorldPawns_Patch.RunDestructivePatches(); //Class was largely overhauled to allow multithreaded ticking for WorldPawns.Tick()


			Alert_MinorBreakRisk_Patch.RunDestructivePatches(); //performance rewrite
            AttackTargetsCache_Patch.RunDestructivesPatches(); //TODO: write ExposeData and change concurrentdictionary
            Building_Door_Patch.RunDestructivePatches(); //strange bug
            CompCauseGameCondition_Patch.RunDestructivePatches(); //TODO - ThreadSafeLinkedList
            CompSpawnSubplant_Transpile.RunDestructivePatches(); //could use interlock instead
            DateNotifier_Patch.RunDestructivePatches(); //performance boost when playing on only 1 map
            DesignationManager_Patch.RunDestructivePatches(); //added for development build
            DrugAIUtility_Patch.RunDestructivePatches(); //vanilla bug
            DynamicDrawManager_Patch.RunDestructivePatches(); //TODO - candidate for ThreadSafeLinkedList?
            //FilthMaker_Patch.RunDestructivePatches(); replaces a few LINQ queries. possible perf improvement
            //GenClosest_Patch.RunDestructivePatches(); replaces RegionwiseBFSWorker - no diff noticable
            //GenCollection_Patch.RunDestructivePatches(); may be fixed now that simplepools work
            GenSpawn_Patch.RunDestructivePatches();
            GenTemperature_Patch.RunDestructivePatches();
            GlobalControlsUtility_Patch.RunDestructivePatches();
            //GrammarResolver_Patch.RunDestructivePatches();
            HediffGiver_Heat_Patch.RunDestructivePatches();
            HediffSet_Patch.RunDestructivePatches();
            ImmunityHandler_Patch.RunDestructivePatches();
            ListerThings_Patch.RunDestructivePatches();
            JobGiver_Work_Patch.RunDestructivePatches();
            JobMaker_Patch.RunDestructivePatches();
            LongEventHandler_Patch.RunDestructivePatches();
            Lord_Patch.RunDestructivePatches();
            LordManager_Patch.RunDestructivePatches();
            LordToil_Siege_Patch.RunDestructivePatches(); //TODO does locks around clears and adds. ThreadSafeLinkedList
			Map_Patch.RunDestructivePatches(); //TODO - discover root cause
            MaterialPool_Patch.RunDestructivePatches();
            MemoryThoughtHandler_Patch.RunDestructivePatches();
            Pawn_HealthTracker_Patch.RunDestructivePatches(); //TODO replace with ThreadSafeLinkedList
            Pawn_MindState_Patch.RunDestructivePatches(); //TODO - destructive hack for speed up - maybe not needed
            Pawn_PlayerSettings_Patch.RunDestructivePatches();
            Pawn_RelationsTracker_Patch.RunDestructivePatches();
			PawnCapacitiesHandler_Patch.RunDestructivePatches();
			PawnPath_Patch.RunDestructivePatches();
			PawnPathPool_Patch.RunDestructivePatches();
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
			SeasonUtility_Patch.RunDestructivePatches(); //performance boost
            ShootLeanUtility_Patch.RunDestructivePatches(); //TODO: excessive locks, therefore RimThreadedHarmony.Prefix, conncurrent_queue could be transpiled in
            SteadyEnvironmentEffects_Patch.RunDestructivePatches();
            StoryState_Patch.RunDestructivePatches(); //WrapMethodInInstanceLock
            TaleManager_Patch.RunDestructivePatches();
			ThingGrid_Patch.RunDestructivePatches();
            ThinkNode_SubtreesByTag_Patch.RunDestructivePatches();
            TileTemperaturesComp_Patch.RunDestructivePatches(); //TODO - good simple transpile candidate
            UniqueIDsManager_Patch.RunDestructivePatches(); // Simple use of Interlocked.Increment
            Verb_Patch.RunDestructivePatches(); // TODO: why is this causing null?
            WealthWatcher_Patch.RunDestructivePatches();
			//WorkGiver_GrowerSow_Patch.RunDestructivePatches();

			//complex methods that need further review for simplification
			AttackTargetReservationManager_Patch.RunDestructivePatches();
			BiomeDef_Patch.RunDestructivePatches();
			FloodFiller_Patch.RunDestructivePatches();//FloodFiller - inefficient global lock - threadstatics might help do these concurrently?
			JobQueue_Patch.RunDestructivePatches();
			MapPawns_Patch.RunDestructivePatches(); //TODO: Affects Animal Master Assignment
			MeditationFocusTypeAvailabilityCache_Patch.RunDestructivePatches();
			Pawn_JobTracker_Patch.RunDestructivePatches();
			Pawn_Patch.RunDestructivePatches();
			Region_Patch.RunDestructivePatches();
			ReservationManager_Patch.RunDestructivePatches();
			Room_Patch.RunDestructivePatches();
			SituationalThoughtHandler_Patch.RunDestructivePatches(); //TODO replace cachedThoughts with ThreadSafeLinkedList
			ThingOwnerUtility_Patch.RunDestructivePatches(); //TODO fix method reference by index

			//-----SOUND-----
			SampleSustainer_Patch.RunDestructivePatches(); // TryMakeAndPlay works better than set_cutoffFrequency, which seems buggy for echo pass filters
			SoundSizeAggregator_Patch.RunDestructivePatches();
			SoundStarter_Patch.RunDestructivePatches(); //disabling this patch stops sounds
			SustainerManager_Patch.RunDestructivePatches();
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
		private static void FullPool_Patch_RunNonDestructivePatches()
		{
			Type original = typeof(FullPool<PawnStatusEffecters.LiveEffecter>);
			Type patched = typeof(FullPool_Patch<PawnStatusEffecters.LiveEffecter>);
			replaceFields.Add(Method(original, nameof(FullPool<PawnStatusEffecters.LiveEffecter>.Get)),
				Method(patched, nameof(FullPool_Patch<PawnStatusEffecters.LiveEffecter>.Get)));
			replaceFields.Add(Method(original, nameof(FullPool<PawnStatusEffecters.LiveEffecter>.Return)),
				Method(patched, nameof(FullPool_Patch<PawnStatusEffecters.LiveEffecter>.Return)));
			replaceFields.Add(Method(original, "get_FreeItemsCount"),
				Method(patched, "get_FreeItemsCount"));

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
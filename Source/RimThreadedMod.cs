using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using UnityEngine;
using System.Reflection.Emit;
using System.Linq;
using System.IO;
using static HarmonyLib.AccessTools;
using System.Security.Permissions;
using System.Security;

namespace RimThreaded 
{ 
    class RimThreadedMod : Mod
    {
        public static RimThreadedSettings Settings;
        public static string replacementsJsonPath;
        public RimThreadedMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<RimThreadedSettings>();
            replacementsJsonPath = Path.Combine(content.RootDir, "1.3", "replacements.json");
            //RimThreaded.Start();
        }
        public override void DoSettingsWindowContents(Rect inRect)
        {
            if (Settings.modsText.Length == 0)
            {
                Settings.modsText = "Potential RimThreaded mod conflicts :\n";
                Settings.modsText += getPotentialModConflicts();

                //Settings.modsText2 = "For future use... \n";
                //Settings.modsText2 += getAllStaticFields();

                //string path = "hmodText.txt";
                //StreamWriter writer = new StreamWriter(path, true);
                //writer.WriteLine(Settings.modsText);
                //writer.Close();
            }
            Settings.DoWindowContents(inRect);
            if (Settings.maxThreads != RimThreaded.maxThreads)
            {
                RimThreaded.maxThreads = Settings.disablelimits ? Math.Max(Settings.maxThreads, 1) : Math.Min(Math.Max(Settings.maxThreads, 1), 255);
                RimThreaded.RestartAllWorkerThreads();
            }
            RimThreaded.timeoutMS = Settings.disablelimits ? Math.Max(Settings.timeoutMS, 1) : Math.Min(Math.Max(Settings.timeoutMS, 10000), 1000000);
            RimThreaded.timeSpeedNormal = Settings.timeSpeedNormal;
            RimThreaded.timeSpeedFast = Settings.timeSpeedFast;
            RimThreaded.timeSpeedSuperfast = Settings.timeSpeedSuperfast;
            RimThreaded.timeSpeedUltrafast = Settings.timeSpeedUltrafast;

        }

        //private string getAllStaticFields()
        //{
        //    string result = "";
        //    HashSet<FieldInfo> fieldInfos = new HashSet<FieldInfo>();
        //    foreach(Type type in Assembly.Load("Assembly-CSharp").GetTypes())
        //    {
        //        foreach(FieldInfo fieldInfo in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
        //        {
        //            fieldInfos.Add(fieldInfo);
        //            //result += type.FullName + " " + fieldInfo.FieldType.Attributes + " " + fieldInfo.Name + "\n";
        //        }
        //    }
        //    foreach (Type type in Assembly.Load("Assembly-CSharp").GetTypes())
        //    {
        //        foreach (MethodInfo methodInfo in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
        //        {
        //            List<CodeInstruction> codeInstructions = PatchProcessor.GetOriginalInstructions(methodInfo, out ILGenerator iLGenerator);
        //            int i = 0;
        //            while(i < codeInstructions.Count)
        //            {
        //                if(codeInstructions[i].opcode == OpCodes.Ldsfld) {
        //                    if (codeInstructions[i + 1].opcode == OpCodes.Call || codeInstructions[i + 1].opcode == OpCodes.Callvirt)
        //                    {
        //                        MethodInfo instructionMethodInfo = (MethodInfo)codeInstructions[i + 1].operand;
        //                        if (instructionMethodInfo.Name.Equals("Clear") && instructionMethodInfo.DeclaringType.FullName.Contains("System.Collections"))
        //                        {
        //                            FieldInfo fieldInfo = (FieldInfo)codeInstructions[i].operand;
        //                            Log.Message(fieldInfo.FieldType.Name + " " + fieldInfo.DeclaringType.FullName + "." + fieldInfo.Name);
        //                        }
        //                    }
        //                }
        //                i++;
        //            }
        //        }
        //    }
        //    MethodBase methodBase = null;
        //    methodBase.GetMethodBody().GetILAsByteArray();
        //    return result;
        //}

        public static string getPotentialModConflicts()
        {
            string modsText = "";
            IEnumerable<MethodBase> originalMethods = Harmony.GetAllPatchedMethods();
            foreach (MethodBase originalMethod in originalMethods)
            {
                Patches patches = Harmony.GetPatchInfo(originalMethod);
                if (patches is null) { }
                else
                {
                    Patch[] sortedPrefixes = patches.Prefixes.ToArray();
                    PatchProcessor.GetSortedPatchMethods(originalMethod, sortedPrefixes);
                    bool isRimThreadedPrefixed = false;
                    string modsText1 = "";
                    foreach (Patch patch in sortedPrefixes)
                    {
                        if (patch.owner.Equals("majorhoff.rimthreaded") && !RimThreadedHarmony.nonDestructivePrefixes.Contains(patch.PatchMethod) && (patches.Prefixes.Count > 1 || patches.Postfixes.Count > 0 || patches.Transpilers.Count > 0))
                        {
                            isRimThreadedPrefixed = true;
                            modsText1 = "\n  ---Patch method: " + patch.PatchMethod.DeclaringType.FullName + " " + patch.PatchMethod + "---\n";
                            modsText1 += "  RimThreaded priority: " + patch.priority + "\n";
                            break;
                        }
                    }
                    if (isRimThreadedPrefixed)
                    {
                        bool rimThreadedPatchFound = false;
                        bool headerPrinted = false;
                        foreach (Patch patch in sortedPrefixes)
                        {
                            if (patch.owner.Equals("majorhoff.rimthreaded"))
                                rimThreadedPatchFound = true;
                            if (!patch.owner.Equals("majorhoff.rimthreaded") && rimThreadedPatchFound)
                            {
                                //Settings.modsText += "method: " + patch.PatchMethod + " - ";
                                if(!headerPrinted)
                                    modsText += modsText1;
                                headerPrinted = true;
                                modsText += "  owner: " + patch.owner + " - ";
                                modsText += "  priority: " + patch.priority + "\n";
                            }
                        }
                        //foreach (Patch patch in patches.Postfixes)
                        //{
                        //    //Settings.modsText += "method: " + patch.PatchMethod + " - ";
                        //    modsText += "  owner: " + patch.owner + " - ";
                        //    modsText += "  priority: " + patch.priority + "\n";
                        //}
                        foreach (Patch patch in patches.Transpilers)
                        {
                            if (!headerPrinted)
                                modsText += modsText1;
                            headerPrinted = true;
                            //Settings.modsText += "method: " + patch.PatchMethod + " - ";
                            modsText += "  owner: " + patch.owner + " - ";
                            modsText += "  priority: " + patch.priority + "\n";
                        }
                    }
                }
            }
            return modsText;
        }
        public static void exportTranspiledMethods()
        {
            AssemblyName aName = new AssemblyName("RimWorldTranspiles");
            //PermissionSet requiredPermission = new PermissionSet(PermissionState.Unrestricted);
            AssemblyBuilder ab = AppDomain.CurrentDomain.DefineDynamicAssembly(aName, AssemblyBuilderAccess.RunAndSave);
            ConstructorInfo Constructor2 = typeof(SecurityPermissionAttribute).GetConstructors()[0];
            SecurityAction requestMinimum = SecurityAction.RequestMinimum;
            PropertyInfo skipVerificationProperty = Property(typeof(SecurityPermissionAttribute), "SkipVerification");
            CustomAttributeBuilder sv2 = new CustomAttributeBuilder(Constructor2, new object[] { requestMinimum }, 
                new PropertyInfo[] { skipVerificationProperty }, new object[] { true });            
            ab.SetCustomAttribute(sv2);

            //System.Security.AllowPartiallyTrustedCallersAttribute Att = new System.Security.AllowPartiallyTrustedCallersAttribute();            
            //ConstructorInfo Constructor1 = Att.GetType().GetConstructors()[0];
            //object[] ObjectArray1 = new object[0];
            //CustomAttributeBuilder AttribBuilder1 = new CustomAttributeBuilder(Constructor1, ObjectArray1);
            //ab.SetCustomAttribute(AttribBuilder1);
            ModuleBuilder modBuilder = ab.DefineDynamicModule(aName.Name, aName.Name + ".dll");
            UnverifiableCodeAttribute ModAtt = new System.Security.UnverifiableCodeAttribute();
            ConstructorInfo Constructor = ModAtt.GetType().GetConstructors()[0];
            object[] ObjectArray = new object[0];
            CustomAttributeBuilder ModAttribBuilder = new CustomAttributeBuilder(Constructor, ObjectArray);
            modBuilder.SetCustomAttribute(ModAttribBuilder);
            Dictionary<string, TypeBuilder> typeBuilders = new Dictionary<string, TypeBuilder>();
            IEnumerable<MethodBase> originalMethods = Harmony.GetAllPatchedMethods();
            foreach (MethodBase originalMethod in originalMethods)
            {
                Patches patches = Harmony.GetPatchInfo(originalMethod);
                int transpiledCount = patches.Transpilers.Count;
                if (transpiledCount > 0)
                {
                    if (originalMethod is MethodInfo methodInfo) // add support for constructors as well
                    {
                        Type returnType = methodInfo.ReturnType;
                        string typeTranspiled = originalMethod.DeclaringType.FullName + "_Transpiled";
                        if (!typeBuilders.TryGetValue(typeTranspiled, out TypeBuilder tb))
                        {
                            tb = modBuilder.DefineType(typeTranspiled, TypeAttributes.Public);
                            typeBuilders[typeTranspiled] = tb;
                        }
                        ParameterInfo[] parameterInfos = methodInfo.GetParameters();
                        List<Type> types = new List<Type>();

                        int parameterOffset = 1;
                        if (!methodInfo.Attributes.HasFlag(MethodAttributes.Static))
                        {
                            types.Add(methodInfo.DeclaringType);
                            parameterOffset = 2;
                        }
                        foreach (ParameterInfo parameterInfo in parameterInfos)
                        {
                            types.Add(parameterInfo.ParameterType);
                        }
                        MethodBuilder mb = tb.DefineMethod(originalMethod.Name, MethodAttributes.Public | MethodAttributes.Static, returnType, types.ToArray());
                        
                        if (!methodInfo.Attributes.HasFlag(MethodAttributes.Static))
                        {
                            ParameterAttributes pa = new ParameterAttributes();
                            ParameterBuilder pb = mb.DefineParameter(1, pa, methodInfo.DeclaringType.Name);
                        }

                        foreach (ParameterInfo parameterInfo in parameterInfos)
                        {
                            ParameterAttributes pa = new ParameterAttributes();
                            if (parameterInfo.IsOut) pa |= ParameterAttributes.Out;
                            if (parameterInfo.IsIn) pa |= ParameterAttributes.In;
                            if (parameterInfo.IsLcid) pa |= ParameterAttributes.Lcid;
                            if (parameterInfo.IsOptional) pa |= ParameterAttributes.Optional;
                            if (parameterInfo.IsRetval) pa |= ParameterAttributes.Retval;
                            if (parameterInfo.HasDefaultValue) pa |= ParameterAttributes.HasDefault;
                            ParameterBuilder pb = mb.DefineParameter(parameterInfo.Position + parameterOffset, pa, parameterInfo.Name);
                            if (parameterInfo.HasDefaultValue && parameterInfo.DefaultValue != null)
                                pb.SetConstant(parameterInfo.DefaultValue);
                        }
                        ILGenerator il = mb.GetILGenerator();
                        List<CodeInstruction> currentInstructions = PatchProcessor.GetCurrentInstructions(originalMethod);
                        Dictionary<Label, Label> labels = new Dictionary<Label, Label>();

                        MethodBody methodBody = methodInfo.GetMethodBody();
                        IList<LocalVariableInfo> localvars = methodBody.LocalVariables;
                        LocalBuilder[] localBuildersOrdered = new LocalBuilder[255];
                        foreach (LocalVariableInfo localVar in localvars)
                        {
                            Type type = localVar.LocalType;
                            LocalBuilder newLocalBuilder = il.DeclareLocal(type);
                            localBuildersOrdered[localVar.LocalIndex] = newLocalBuilder;
                        }
                        IList<ExceptionHandlingClause> exceptionHandlingClauses = methodBody.ExceptionHandlingClauses;

                        //LocalBuilder[] localBuildersOrdered = new LocalBuilder[255];                        
                        //int localBuildersOrderedMax = 0;
                        //foreach (CodeInstruction currentInstruction in currentInstructions)
                        //{
                        //    object operand = currentInstruction.operand;
                        //    if (operand is LocalBuilder localBuilder)
                        //    {
                        //        localBuildersOrdered[localBuilder.LocalIndex] = localBuilder;
                        //        localBuildersOrderedMax = Math.Max(localBuildersOrderedMax, localBuilder.LocalIndex);
                        //    }
                        //}
                        //Dictionary<LocalBuilder, LocalBuilder> localBuilders = new Dictionary<LocalBuilder, LocalBuilder>();
                        //for (int i = 0; i <= localBuildersOrderedMax; i++)
                        //{
                        //    LocalBuilder localBuilderOrdered = localBuildersOrdered[i];
                        //    if (localBuilderOrdered == null)
                        //    {
                        //        il.DeclareLocal(typeof(object));
                        //    }
                        //    else
                        //    {
                        //        LocalBuilder newLocalBuilder = il.DeclareLocal(localBuilderOrdered.LocalType);
                        //        localBuilders.Add(localBuilderOrdered, newLocalBuilder);
                        //    }
                        //}
                        foreach (CodeInstruction currentInstruction in currentInstructions)
                        {
                            foreach (Label label in currentInstruction.labels)
                            {
                                if (!labels.TryGetValue(label, out Label translatedLabel))
                                {
                                    translatedLabel = il.DefineLabel();
                                    labels[label] = translatedLabel;
                                }
                                il.MarkLabel(translatedLabel);
                            }

                            int i = il.ILOffset;
                            foreach (ExceptionHandlingClause Clause in exceptionHandlingClauses)
                            {
                                if (Clause.Flags != ExceptionHandlingClauseOptions.Clause &&
                                   Clause.Flags != ExceptionHandlingClauseOptions.Finally)
                                    continue;

                                // Look for an ending of an exception block first!
                                if (Clause.HandlerOffset + Clause.HandlerLength == i)
                                    il.EndExceptionBlock();

                                // If this marks the beginning of a try block, emit that
                                if (Clause.TryOffset == i)
                                    il.BeginExceptionBlock();

                                // Also check for the beginning of a catch block
                                if (Clause.HandlerOffset == i && Clause.Flags == ExceptionHandlingClauseOptions.Clause)
                                    il.BeginCatchBlock(Clause.CatchType);

                                // Lastly, check for a finally block
                                if (Clause.HandlerOffset == i && Clause.Flags == ExceptionHandlingClauseOptions.Finally)
                                    il.BeginFinallyBlock();
                            }


                            OpCode opcode = currentInstruction.opcode;
                            object operand = currentInstruction.operand;
                            switch (operand)
                            {
                                case null:
                                    {
                                        il.Emit(opcode);
                                        break;
                                    }
                                case byte operandCasted:
                                    {
                                        il.Emit(opcode, operandCasted);
                                        break;
                                    }
                                case sbyte operandCasted:
                                    {
                                        il.Emit(opcode, operandCasted);
                                        break;
                                    }
                                case short operandCasted:
                                    {
                                        il.Emit(opcode, operandCasted);
                                        break;
                                    }
                                case int operandCasted:
                                    {
                                        il.Emit(opcode, operandCasted);
                                        break;
                                    }
                                case MethodInfo operandCasted:
                                    {
                                        il.Emit(opcode, operandCasted);
                                        break;
                                    }
                                case SignatureHelper operandCasted:
                                    {
                                        il.Emit(opcode, operandCasted);
                                        break;
                                    }
                                case ConstructorInfo operandCasted:
                                    {
                                        il.Emit(opcode, operandCasted);
                                        break;
                                    }
                                case Type operandCasted:
                                    {
                                        il.Emit(opcode, operandCasted);
                                        break;
                                    }
                                case long operandCasted:
                                    {
                                        il.Emit(opcode, operandCasted);
                                        break;
                                    }
                                case float operandCasted:
                                    {
                                        il.Emit(opcode, operandCasted);
                                        break;
                                    }
                                case double operandCasted:
                                    {
                                        il.Emit(opcode, operandCasted);
                                        break;
                                    }
                                case Label operandCasted:
                                    {
                                        if (!labels.TryGetValue(operandCasted, out Label translatedLabel))
                                        {
                                            translatedLabel = il.DefineLabel();
                                            labels[operandCasted] = translatedLabel;
                                        }
                                        il.Emit(opcode, translatedLabel);
                                        break;
                                    }
                                case Label[] operandCasted:
                                    {
                                        List<Label> newLabels = new List<Label>();
                                        foreach (Label operandCasted1 in operandCasted)
                                        {
                                            if (!labels.TryGetValue(operandCasted1, out Label translatedLabel))
                                            {
                                                translatedLabel = il.DefineLabel();
                                                labels[operandCasted1] = translatedLabel;
                                            }
                                            newLabels.Add(translatedLabel);
                                        }
                                        il.Emit(opcode, newLabels.ToArray());
                                        break;
                                    }
                                case FieldInfo operandCasted:
                                    {
                                        il.Emit(opcode, operandCasted);
                                        break;
                                    }
                                case string operandCasted:
                                    {
                                        il.Emit(opcode, operandCasted);
                                        break;
                                    }
                                case LocalBuilder operandCasted:
                                    {
                                        il.Emit(opcode, localBuildersOrdered[operandCasted.LocalIndex]);
                                        break;
                                    }
                                default:
                                    {
                                        Log.Error("UNKNOWN OPERAND");
                                        break;
                                    }
                            }
                        }
                    }
                }
            }
            foreach (KeyValuePair<string, TypeBuilder> tb in typeBuilders)
            {
                
                tb.Value.CreateType();
            }
            ab.Save(aName.Name + ".dll");
            

            //ReImport DLL and create detour
            Assembly.UnsafeLoadFrom(aName.Name + ".dll");
            foreach (MethodBase originalMethod in originalMethods)
            {
                Patches patches = Harmony.GetPatchInfo(originalMethod);
                int transpiledCount = patches.Transpilers.Count;
                if (transpiledCount > 0)
                {
                    if (originalMethod is MethodInfo methodInfo) // add support for constructors as well
                    {
                        Type transpiledType = TypeByName(originalMethod.DeclaringType.FullName + "_Transpiled");
                        ParameterInfo[] parameterInfos = methodInfo.GetParameters();
                        List<Type> types = new List<Type>();

                        if (!methodInfo.Attributes.HasFlag(MethodAttributes.Static))
                        {
                            types.Add(methodInfo.DeclaringType);
                        }
                        foreach (ParameterInfo parameterInfo in parameterInfos)
                        {
                            types.Add(parameterInfo.ParameterType);
                        }
                        MethodInfo replacement = Method(transpiledType, originalMethod.Name, types.ToArray());
                        Memory.DetourMethod(originalMethod, replacement);
                    }
                }
            }
        }

        public override string SettingsCategory()
        {
            return "RimThreaded";
        }

    }

}


using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Mono.Cecil.Cil;
using UnityEngine;
using Verse;
using static HarmonyLib.AccessTools;
using static RimThreaded.RimThreadedHarmony;
using static System.Threading.Thread;

namespace RimThreaded.Mod_Patches
{
    class CombatExteneded_Patch
    {
        public static Type patched = typeof(CombatExteneded_Patch);
        public static Type combatExtendedCE_Utility;
		public static Type combatExtendedVerb_LaunchProjectileCE;
		public static Type combatExtendedVerb_MeleeAttackCE;
		public static Type combatExtended_ProjectileCE;


		public static void Patch()
        {
			combatExtendedCE_Utility = TypeByName("CombatExtended.CE_Utility");
			combatExtendedVerb_LaunchProjectileCE = TypeByName("CombatExtended.Verb_LaunchProjectileCE");
			combatExtendedVerb_MeleeAttackCE = TypeByName("CombatExtended.Verb_MeleeAttackCE");
			combatExtended_ProjectileCE = TypeByName("CombatExtended.ProjectileCE");
            
			//if (combatExtendedCE_Utility != null)
			//{
			//	string methodName = nameof(BlitCrop);
			//	Log.Message("RimThreaded is patching " + combatExtendedCE_Utility.FullName + " " + methodName);
			//	Transpile(combatExtendedCE_Utility, patched, methodName);
			//	methodName = nameof(GetColorSafe);
			//	Log.Message("RimThreaded is patching " + combatExtendedCE_Utility.FullName + " " + methodName);
			//	Transpile(combatExtendedCE_Utility, patched, methodName);
			//}


			Type CE_ThingsTrackingModel = TypeByName("CombatExtended.Utilities.ThingsTrackingModel");
			if (CE_ThingsTrackingModel != null)
            {
				string methodName = "Register";
				Log.Message("RimThreaded is patching " + CE_ThingsTrackingModel.FullName + " " + methodName);
				RimThreadedHarmony.TranspileMethodLock(CE_ThingsTrackingModel, methodName);
				string methodName2 = "DeRegister";
				Log.Message("RimThreaded is patching " + CE_ThingsTrackingModel.FullName + " " + methodName2);
				RimThreadedHarmony.TranspileMethodLock(CE_ThingsTrackingModel, methodName2);
				string methodName3 = "Notify_ThingPositionChanged";
				Log.Message("RimThreaded is patching " + CE_ThingsTrackingModel.FullName + " " + methodName3);
				RimThreadedHarmony.TranspileMethodLock(CE_ThingsTrackingModel, methodName3);//lock should be reentrant otherwise this is an obvious deadlock.
				//string methodName4 = "ThingsInRangeOf";ThingsInRangeOf
				//Log.Message("RimThreaded is patching " + CE_ThingsTrackingModel.FullName + " " + methodName4);
				//RimThreadedHarmony.TranspileMethodLock(CE_ThingsTrackingModel, methodName4);
			}
            Type CompTacticalManagerType = TypeByName("CombatExtended.CompTacticalManager");
            if (CompTacticalManagerType != null)
            {
                string methodName = nameof(CompTick);
                Log.Message("RimThreaded is patching " + CompTacticalManagerType.FullName + " " + methodName);
				Transpile(CompTacticalManagerType, patched, methodName);
			}

			//if (combatExtendedVerb_LaunchProjectileCE != null)
			//{
			//	string methodName = "CanHitFromCellIgnoringRange";
			//	patched = typeof(Verb_LaunchProjectileCE_Transpile);
			//	Log.Message("RimThreaded is patching " + combatExtendedVerb_LaunchProjectileCE.FullName + " " + methodName);
			//	Transpile(combatExtendedVerb_LaunchProjectileCE, patched, methodName);
			//	methodName = "TryFindCEShootLineFromTo";
			//	Log.Message("RimThreaded is patching " + combatExtendedVerb_LaunchProjectileCE.FullName + " " + methodName);
			//	Transpile(combatExtendedVerb_LaunchProjectileCE, patched, methodName);
			//}
			//if (combatExtendedVerb_MeleeAttackCE != null)
			//{
			//	string methodName = "TryCastShot";
			//	patched = typeof(Verb_MeleeAttackCE_Transpile);
			//	Log.Message("RimThreaded is patching " + combatExtendedVerb_MeleeAttackCE.FullName + " " + methodName);
			//	Transpile(combatExtendedVerb_MeleeAttackCE, patched, methodName);
			//}

		}

        public static IEnumerable<CodeInstruction> CompTick(IEnumerable<CodeInstruction> instructions,
            ILGenerator iLGenerator)
        {
            List<CodeInstruction> codeInstructions = instructions.ToList();
            for (int i = 0; i < codeInstructions.Count; i++)
            {
                CodeInstruction ci = codeInstructions[i];
                if (ci.opcode == OpCodes.Ldfld && (FieldInfo) ci.operand == Field(typeof(JobDef), "alwaysShowWeapon"))
                {
                    LocalBuilder jobDef = iLGenerator.DeclareLocal(typeof(JobDef));
                    yield return new CodeInstruction(OpCodes.Stloc, jobDef);
                    yield return new CodeInstruction(OpCodes.Ldloc, jobDef);
                    Label label = (Label) codeInstructions[i - 3].operand;
                    yield return new CodeInstruction(OpCodes.Brfalse, label);
                    yield return new CodeInstruction(OpCodes.Ldloc, jobDef);
                }

                yield return ci;
            }
        }

        public static IEnumerable<CodeInstruction> BlitCrop(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            List<CodeInstruction> instructionsList = instructions.ToList();
            int currentInstructionIndex = 0;
            int matchFound = 0;
            while (currentInstructionIndex < instructionsList.Count)
            {
                CodeInstruction codeInstruction = instructionsList[currentInstructionIndex];
                if (codeInstruction.opcode == OpCodes.Call &&
                    (MethodInfo)codeInstruction.operand == AccessTools.Method(CombatExteneded_Patch.combatExtendedCE_Utility, "Blit"))
                {
                    matchFound++;
                    codeInstruction.operand = AccessTools.Method(typeof(CE_Utility_Transpile), "Blit");
                }
                yield return codeInstruction;
                currentInstructionIndex++;
            }
            if (matchFound < 1)
            {
                Log.Error("IL code instructions not found");
            }
        }
        public static IEnumerable<CodeInstruction> GetColorSafe(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            List<CodeInstruction> instructionsList = instructions.ToList();
            int currentInstructionIndex = 0;
            int matchFound = 0;
            while (currentInstructionIndex < instructionsList.Count)
            {
                CodeInstruction codeInstruction = instructionsList[currentInstructionIndex];
                if (codeInstruction.opcode == OpCodes.Call && codeInstruction.operand is MethodInfo methodInfo &&
                    methodInfo == AccessTools.Method(CombatExteneded_Patch.combatExtendedCE_Utility, "Blit"))
                {
                    matchFound++;
                    codeInstruction.operand = AccessTools.Method(typeof(CE_Utility_Transpile), "Blit");
                }
                yield return codeInstruction;
                currentInstructionIndex++;
            }
            if (matchFound < 1)
            {
                Log.Error("IL code instructions not found");
            }
        }

        static readonly Func<object[], object> safeFunction = parameters =>
            SafeBlit(
                (Texture2D)parameters[0],
                (Rect)parameters[1],
                (int[])parameters[2]);

        public static Texture2D Blit(Texture2D texture, Rect blitRect, int[] rtSize)
        {
            if (!CurrentThread.IsBackground || !RimThreaded.allWorkerThreads.TryGetValue(CurrentThread, out RimThreaded.ThreadInfo threadInfo))
                return SafeBlit(texture, blitRect, rtSize);
            threadInfo.safeFunctionRequest = new object[] { safeFunction, new object[] { texture, blitRect, rtSize } };
            RimThreaded.mainThreadWaitHandle.Set();
            threadInfo.eventWaitStart.WaitOne();
            return (Texture2D)threadInfo.safeFunctionResult;
        }

        public static Texture2D SafeBlit(Texture2D texture, Rect blitRect, int[] rtSize)
        {
            FilterMode filterMode = texture.filterMode;
            texture.filterMode = FilterMode.Point;
            RenderTexture temporary = RenderTexture.GetTemporary(rtSize[0], rtSize[1], 0, RenderTextureFormat.Default, RenderTextureReadWrite.Default, 1);
            temporary.filterMode = FilterMode.Point;
            RenderTexture.active = temporary;
            Graphics.Blit(texture, temporary);
            Texture2D texture2D = new Texture2D((int)blitRect.width, (int)blitRect.height);
            texture2D.ReadPixels(blitRect, 0, 0);
            texture2D.Apply();
            RenderTexture.active = null;
            texture.filterMode = filterMode;
            return texture2D;
        }
    }
}

using HarmonyLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using Verse;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{
    class PawnTextureAtlas_Patch
    {
		[ThreadStatic] public static List<Pawn> tmpPawnsToFree = new List<Pawn>();
		public static Dictionary<PawnTextureAtlas, ConcurrentStack<PawnTextureAtlasFrameSet>> PawnTextureAtlas_To_FreeFrameSets = new Dictionary<PawnTextureAtlas, ConcurrentStack<PawnTextureAtlasFrameSet>>();

		static Type original = typeof(PawnTextureAtlas);
		static Type patched = typeof(PawnTextureAtlas_Patch);

		public static void RunDestructivePatches()
		{
			RimThreadedHarmony.Prefix(original, patched, nameof(get_FreeCount));
			RimThreadedHarmony.Prefix(original, patched, nameof(TryGetFrameSet));
			RimThreadedHarmony.Prefix(original, patched, nameof(GC));
			RimThreadedHarmony.Prefix(original, patched, nameof(Destroy));
			HarmonyMethod transpilerMethod = new HarmonyMethod(Method(patched, nameof(PawnTextureAtlas)));
			RimThreadedHarmony.harmony.Patch(Constructor(original), transpiler: transpilerMethod);
		}

		internal static void InitializeThreadStatics()
		{
			tmpPawnsToFree = new List<Pawn>();
		}
		public static ConcurrentStack<PawnTextureAtlasFrameSet> getFreeFrameSets(PawnTextureAtlas pawnTextureAtlas)
        {
			if(!PawnTextureAtlas_To_FreeFrameSets.TryGetValue(pawnTextureAtlas, out ConcurrentStack<PawnTextureAtlasFrameSet> freeFrameSets)) {
				lock(PawnTextureAtlas_To_FreeFrameSets)
                {
					if (!PawnTextureAtlas_To_FreeFrameSets.TryGetValue(pawnTextureAtlas, out ConcurrentStack<PawnTextureAtlasFrameSet> freeFrameSets2)) {
						freeFrameSets = new ConcurrentStack<PawnTextureAtlasFrameSet>();
						PawnTextureAtlas_To_FreeFrameSets[pawnTextureAtlas] = freeFrameSets;
					} else
                    {
						freeFrameSets = freeFrameSets2;
					}
				}
            }
			return freeFrameSets;
		}

		public static IEnumerable<CodeInstruction> PawnTextureAtlas(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
		{
			List<CodeInstruction> instructionsList = instructions.ToList();
			for(int i = 0; i < instructionsList.Count; i++)
            {
				CodeInstruction instruction = instructionsList[i];
				if(instruction.opcode == OpCodes.Ldfld && (FieldInfo)instruction.operand == Field(original, "freeFrameSets"))
				{
					yield return new CodeInstruction(OpCodes.Call, Method(patched, nameof(getFreeFrameSets)));
					yield return new CodeInstruction(OpCodes.Ldloc_3);
					yield return new CodeInstruction(OpCodes.Callvirt, Method(typeof(ConcurrentStack<PawnTextureAtlasFrameSet>), "Push"));
					i += 2;
				} else
                {
					yield return instruction;
                }
			}
		}

		public static bool get_FreeCount(PawnTextureAtlas __instance, ref int __result)
        {
			__result = getFreeFrameSets(__instance).Count;
			return false;
		}


		public static bool TryGetFrameSet(PawnTextureAtlas __instance, ref bool __result, Pawn pawn, out PawnTextureAtlasFrameSet frameSet, out bool createdNew)
		{
			createdNew = false;
			if (!__instance.frameAssignments.TryGetValue(pawn, out frameSet))
			{
                ConcurrentStack<PawnTextureAtlasFrameSet> freeFrameSets = getFreeFrameSets(__instance);
				if (freeFrameSets.Count == 0)
				{
					__result = false;
					return false;
				}
				createdNew = true;
				freeFrameSets.TryPop(out frameSet);
				for (int i = 0; i < frameSet.isDirty.Length; i++)
				{
					frameSet.isDirty[i] = true;
				}
				lock (__instance.frameAssignments)
				{
					__instance.frameAssignments.Add(pawn, frameSet); //maybe needs copy
				}
				__result = true;
				return false;
			}
			__result = true;
			return false;
		}

		public static bool GC(PawnTextureAtlas __instance)
		{
			try
			{
				foreach (Pawn key in __instance.frameAssignments.Keys.ToArray())
				{
					if (!key.SpawnedOrAnyParentSpawned)
					{
						tmpPawnsToFree.Add(key);
					}
				}
				ConcurrentStack<PawnTextureAtlasFrameSet> freeFrameSets = getFreeFrameSets(__instance);

				lock (__instance.frameAssignments)
				{
					Dictionary<Pawn, PawnTextureAtlasFrameSet> frameAssignmentsCopy = new Dictionary<Pawn, PawnTextureAtlasFrameSet>(__instance.frameAssignments);
					foreach (Pawn item in tmpPawnsToFree)
					{
						freeFrameSets.Push(__instance.frameAssignments[item]);
						frameAssignmentsCopy.Remove(item);
					}
					__instance.frameAssignments = frameAssignmentsCopy;
				}

			}
			finally
			{
				tmpPawnsToFree.Clear();
			}
			return false;
		}
		public static bool Destroy(PawnTextureAtlas __instance)
		{
			ConcurrentStack<PawnTextureAtlasFrameSet> freeFrameSets = getFreeFrameSets(__instance);
			foreach (PawnTextureAtlasFrameSet item in __instance.frameAssignments.Values.Concat(freeFrameSets))
			{
				Mesh[] meshes = item.meshes;
				for (int i = 0; i < meshes.Length; i++)
				{
					UnityEngine.Object.Destroy(meshes[i]);
				}
			}
			lock (__instance.frameAssignments)
			{
				__instance.frameAssignments = new Dictionary<Pawn, PawnTextureAtlasFrameSet>();
			}
			freeFrameSets.Clear();
			UnityEngine.Object.Destroy(__instance.texture);
			return false;
		}
	}
}

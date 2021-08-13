using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using static HarmonyLib.AccessTools;
using static RimThreaded.PlantHarvest_Cache;

namespace RimThreaded
{
    class Plant_Patch
    {
        internal static void RunNonDestructivePatches()
        {
            Type original = typeof(Plant);
            Type patched = typeof(Plant_Patch);
            RimThreadedHarmony.Postfix(original, patched, nameof(PlantCollected));
            RimThreadedHarmony.Postfix(original, patched, nameof(set_Growth));
            RimThreadedHarmony.Transpile(original, patched, nameof(TickLong));
        }

        public static void PlantCollected(Plant __instance, Pawn by)
        {
            JumboCell.ReregisterObject(__instance.Map, __instance.Position, RimThreaded.plantHarvest_Cache);
        }
        public static void set_Growth(Plant __instance, float value)
        {
            if (__instance.Map != null && __instance.LifeStage == PlantLifeStage.Mature)
                JumboCell.ReregisterObject(__instance.Map, __instance.Position, RimThreaded.plantHarvest_Cache);
        }

        public static IEnumerable<CodeInstruction> TickLong(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
		{
			foreach(CodeInstruction instruction in instructions)
            {
				if(instruction.opcode == OpCodes.Callvirt && 
					(MethodInfo)instruction.operand == Method(typeof(MapDrawer), "MapMeshDirty", new Type[] { typeof(IntVec3), typeof(MapMeshFlag) })) {
					yield return instruction;
                    CodeInstruction ci1 = new CodeInstruction(OpCodes.Ldarg_0);
                    yield return ci1;
                    CodeInstruction ci2 = new CodeInstruction(OpCodes.Call, Method(typeof(Thing), "get_Map"));
                    yield return ci2;
                    CodeInstruction ci3 = new CodeInstruction(OpCodes.Ldarg_0);
                    yield return ci3;
                    CodeInstruction ci4 = new CodeInstruction(OpCodes.Call, Method(typeof(Thing), "get_Position"));
                    yield return ci4;
                    CodeInstruction ci5 = new CodeInstruction(OpCodes.Ldsfld, Field(typeof(RimThreaded), nameof(RimThreaded.plantHarvest_Cache)));
                    yield return ci5;
                    CodeInstruction ci6 = new CodeInstruction(OpCodes.Call, Method(typeof(JumboCell), nameof(JumboCell.ReregisterObject)));
                    yield return ci6;
                    continue;
				}
                yield return instruction;
            }
		}
	}
}

using HarmonyLib;
using System;
using System.Reflection;
using Verse;
using System.Collections.Generic;
using System.Reflection.Emit;
using static HarmonyLib.AccessTools;
using static RimThreaded.RimThreadedHarmony;
using System.Linq;

namespace RimThreaded.Mod_Patches
{
    class RimWar_Patch
    {
        public static Type RimWar_Planet_WorldUtility;
        public static void Patch()
        {
            RimWar_Planet_WorldUtility = TypeByName("RimWar.Planet.WorldUtility");
            if (RimWar_Planet_WorldUtility != null)
            {
                string methodName = nameof(GetWorldObjectsInRange);
                Log.Message("RimThreaded is patching " + RimWar_Planet_WorldUtility.FullName + " " + methodName);
                Transpile(RimWar_Planet_WorldUtility, typeof(RimWar_Patch), methodName);
            }
        }

        public static IEnumerable<CodeInstruction> GetWorldObjectsInRange(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            List<CodeInstruction> instructionsList = instructions.ToList();
            for(int i = 0; i < instructionsList.Count; i++)
            {
                CodeInstruction codeInstruction = instructionsList[i];
                if (codeInstruction.opcode == OpCodes.Callvirt && 
                    (MethodInfo)codeInstruction.operand == Method(typeof(RimWorld.Planet.WorldObject), "get_Tile"))
                {
                    LocalBuilder worldObject = iLGenerator.DeclareLocal(typeof(RimWorld.Planet.WorldObject));
                    yield return new CodeInstruction(OpCodes.Stloc, worldObject);
                    yield return new CodeInstruction(OpCodes.Ldloc, worldObject);
                    //yield return new CodeInstruction(OpCodes.Ldnull);
                    //yield return new CodeInstruction(OpCodes.Ceq);
                    Label label = (Label)instructionsList[i + 10].operand;
                    //yield return new CodeInstruction(OpCodes.Brtrue, label);
                    yield return new CodeInstruction(OpCodes.Brfalse, label);
                    yield return new CodeInstruction(OpCodes.Ldloc, worldObject);
                }
                yield return codeInstruction;
            }
        }
    }
}

using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Verse;
using System.Reflection.Emit;
using System;
using RimWorld.Planet;

using System.Threading;
using System.Reflection;

namespace RimThreaded
{
    public class TileTemperaturesComp_Transpile
    {
        public static IEnumerable<CodeInstruction> WorldComponentTick(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            List<CodeInstruction> instructionsList = instructions.ToList();
            int i = 0;

			LocalBuilder usedSlot = iLGenerator.DeclareLocal(AccessTools.TypeByName("RimWorld.Planet.TileTemperaturesComp+CachedTileTemperatureData"));
            Label startLoop = iLGenerator.DefineLabel();

            while (i < instructionsList.Count)
            {
                if (i + 7 < instructionsList.Count && instructionsList[i + 7].opcode == OpCodes.Ldelem_Ref)                    
                {
                    instructionsList[i].labels.Add(startLoop);
                    yield return instructionsList[i];
                    i++;

                }
                else if (instructionsList[i].opcode == OpCodes.Ldelem_Ref)
                {
                    yield return instructionsList[i];
                    i++;
                    yield return (new CodeInstruction(OpCodes.Stloc, usedSlot.LocalIndex));
                    yield return (new CodeInstruction(OpCodes.Ldloc, usedSlot.LocalIndex));
                    yield return (new CodeInstruction(OpCodes.Brfalse, startLoop));
                    yield return (new CodeInstruction(OpCodes.Ldloc, usedSlot.LocalIndex));
                }
                else
                {
                    yield return instructionsList[i];
                    i++;
                }
            }
        }
    }
}

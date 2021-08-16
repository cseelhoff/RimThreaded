using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace RimThreaded.Mod_Patches
{
    public class JobDriver_Mounted_Transpile
    {
        //public static IEnumerable<CodeInstruction> WaitForRider(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        //{
        //    List<CodeInstruction> searchInstructions2 = new List<CodeInstruction>
        //    {
        //        new CodeInstruction(OpCodes.Ldarg_0),
        //        new CodeInstruction(OpCodes.Call, AccessTools.Method(GiddyUpCore_Patch.giddyUpCoreJobsJobDriver_Mounted, "get_Rider")),
        //        new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Pawn), "get_CurJob")),
        //    };
        //    List<CodeInstruction> searchInstructions = searchInstructions2.ListFullCopy();
        //    searchInstructions.Add(new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Job), "def")));
        //    searchInstructions.Add(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(GiddyUpCore_Patch.giddyUpCoreJobsGUC_JobDefOf, "Mount")));
        //    searchInstructions.Add(new CodeInstruction(OpCodes.Beq_S));

        //    List<CodeInstruction> instructionsList = instructions.ToList();
        //    int currentInstructionIndex = 0;
        //    bool matchFound = false;
        //    LocalBuilder curJob = iLGenerator.DeclareLocal(typeof(Job));
        //    while (currentInstructionIndex < instructionsList.Count)
        //    {
        //        if (RimThreadedHarmony.IsCodeInstructionsMatching(searchInstructions, instructionsList, currentInstructionIndex))
        //        {
        //            matchFound = true;
        //            for (int i = 0; i < 3; i++)
        //            {
        //                yield return instructionsList[currentInstructionIndex];
        //                currentInstructionIndex++;
        //            }
                    
        //            yield return new CodeInstruction(OpCodes.Stloc, curJob.LocalIndex);
        //            yield return new CodeInstruction(OpCodes.Ldloc, curJob.LocalIndex);
        //            yield return new CodeInstruction(OpCodes.Brfalse_S, 
        //                instructionsList[currentInstructionIndex + 2].operand); //this may need to be a jump to a line 5 lines above this
        //            yield return new CodeInstruction(OpCodes.Ldloc, curJob.LocalIndex);
        //            for (int i = 0; i < 3; i++)
        //            {
        //                yield return instructionsList[currentInstructionIndex];
        //                currentInstructionIndex++;
        //            }
        //        }
        //        else if (RimThreadedHarmony.IsCodeInstructionsMatching(searchInstructions2, instructionsList, currentInstructionIndex))
        //        {
        //            matchFound = true;
        //            yield return new CodeInstruction(OpCodes.Ldloc, curJob.LocalIndex);
        //            for (int i = 0; i < 3; i++)
        //            {
        //                //yield return instructionsList[currentInstructionIndex];
        //                currentInstructionIndex++;
        //            }
        //        }
        //        else
        //        {
        //            yield return instructionsList[currentInstructionIndex];
        //            currentInstructionIndex++;
        //        }
        //    }
        //    if (!matchFound)
        //    {
        //        Log.Error("IL code instructions not found");
        //    }
        //}
    }
}

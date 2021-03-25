using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace RimThreaded
{
    class DubsSkylight_getPatch_Transpile
    {

        public static IEnumerable<CodeInstruction> Postfix(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            // Local field and Label Declaration
            iLGenerator.DeclareLocal(typeof(Room));
            Label il_002b = iLGenerator.DefineLabel();

            // Room room = c.getRoom(map)
            yield return new CodeInstruction(OpCodes.Ldarg_0);
            yield return new CodeInstruction(OpCodes.Ldarg_1);
            yield return new CodeInstruction(OpCodes.Ldc_I4_6);
            yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(GridsUtility), "GetRoom"));
            yield return new CodeInstruction(OpCodes.Stloc_0);

            // room != null
            yield return new CodeInstruction(OpCodes.Ldloc_0);
            yield return new CodeInstruction(OpCodes.Brfalse_S, il_002b);

            //room.Role != null
            yield return new CodeInstruction(OpCodes.Ldloc_0);
            yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Room), "get_Role"));
            yield return new CodeInstruction(OpCodes.Brfalse_S, il_002b);

            //relt == true
            yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(AccessTools.TypeByName("Dubs_Skylight.Patch_GetRoof"), "relt"));
            yield return new CodeInstruction(OpCodes.Brfalse_S, il_002b);

            //room.Role == IndoorGarden
            yield return new CodeInstruction(OpCodes.Ldloc_0);
            yield return new CodeInstruction(OpCodes.Callvirt, AccessTools.Method(typeof(Room), "get_Role"));
            yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(AccessTools.TypeByName("Dubs_Skylight.DubDef"), "IndoorGarden"));
            yield return new CodeInstruction(OpCodes.Bne_Un_S, il_002b);

            // __Result = null
            yield return new CodeInstruction(OpCodes.Ldarg_2);
            yield return new CodeInstruction(OpCodes.Ldnull);
            yield return new CodeInstruction(OpCodes.Stind_Ref);

            // Return with label
            CodeInstruction ci = new CodeInstruction(OpCodes.Ret);
            ci.labels.Add(il_002b);
            yield return ci;
        }

    }
}

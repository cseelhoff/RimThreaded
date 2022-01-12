using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Verse;
using HarmonyLib;
using static HarmonyLib.AccessTools;
using static RimThreaded.RimThreadedHarmony;

namespace RimThreaded.Mod_Patches
{
    class Better_Message_Placement_Patch
    {

        public static void Patch()
        {
            Type Messages_MessagesDoGUI_Patch = TypeByName("Better_Message_Placement.Messages_MessagesDoGUI_Patch");
            if (Messages_MessagesDoGUI_Patch != null)
            {

                string methodName = "MessagesDoGUI";
                Log.Message("RimThreaded is patching " + typeof(Messages_Patch).FullName + " " + methodName);
                Transpile(typeof(Messages_Patch), Messages_MessagesDoGUI_Patch, methodName, patchMethod: "Transpiler");
            }
        }
    }
}
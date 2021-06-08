using HarmonyLib;
using System;
using System.Reflection;
using Verse;
using static HarmonyLib.AccessTools;
using static RimThreaded.RimThreadedHarmony;

namespace RimThreaded.Mod_Patches
{
    class Dubs_Skylight_Patch
    {
        public static Type dubsSkylight_Patch_GetRoof;
        public static void Patch()
        {
            dubsSkylight_Patch_GetRoof = TypeByName("Dubs_Skylight.Patch_GetRoof");
            Type patched;
            if (dubsSkylight_Patch_GetRoof != null)
            {
                string methodName = "Postfix";
                patched = typeof(DubsSkylight_getPatch_Transpile);
                Log.Message("RimThreaded is patching " + dubsSkylight_Patch_GetRoof.FullName + " " + methodName);
                Transpile(dubsSkylight_Patch_GetRoof, patched, methodName);
            }
        }
    }
}

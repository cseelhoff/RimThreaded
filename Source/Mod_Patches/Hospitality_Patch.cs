using System;
using Verse;
using static HarmonyLib.AccessTools;
using static RimThreaded.RimThreadedHarmony;

namespace RimThreaded.Mod_Patches
{
    class Hospitality_Patch
    {
        public static Type hospitalityCompUtility;
        public static Type hospitalityCompGuest;
        public static void Patch()
        {
            hospitalityCompUtility = TypeByName("Hospitality.CompUtility");
            hospitalityCompGuest = TypeByName("Hospitality.CompGuest");

            Type patched;
            if (hospitalityCompUtility != null)
            {
                string methodName = "CompGuest";
                Log.Message("RimThreaded is patching " + hospitalityCompUtility.FullName + " " + methodName);
                patched = typeof(CompUtility_Transpile);
                Transpile(hospitalityCompUtility, patched, methodName);
                methodName = "OnPawnRemoved";
                Log.Message("RimThreaded is patching " + hospitalityCompUtility.FullName + " " + methodName);
                Transpile(hospitalityCompUtility, patched, methodName);
            }
        }
    }
}

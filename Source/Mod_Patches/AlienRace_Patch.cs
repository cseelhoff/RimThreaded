
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
    class AlienRace_Patch
    {

        public static void Patch()
        {
            Type ARHarmonyPatches = TypeByName("AlienRace.HarmonyPatches");
            if (ARHarmonyPatches != null)
            {

                string methodName = nameof(HediffSet_Patch.AddDirect);
                Log.Message("RimThreaded is patching " + typeof(HediffSet_Patch).FullName + " " + methodName);
                Transpile(typeof(HediffSet_Patch), typeof(AlienRace_Patch), methodName);


                methodName = nameof(HediffSet_Patch.CacheMissingPartsCommonAncestors);
                Log.Message("RimThreaded is patching " + typeof(HediffSet_Patch).FullName + " " + methodName);
                Transpile(typeof(HediffSet_Patch), typeof(AlienRace_Patch), methodName);

            }
        }


        public static IEnumerable<CodeInstruction> AddDirect(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {

            Type ARHarmonyPatches = TypeByName("AlienRace.HarmonyPatches");
            return (IEnumerable<CodeInstruction>)ARHarmonyPatches.GetMethod("BodyReferenceTranspiler").Invoke(null, new object[] { instructions });
        }
        public static IEnumerable<CodeInstruction> CacheMissingPartsCommonAncestors(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {

            Type ARHarmonyPatches = TypeByName("AlienRace.HarmonyPatches");
            return (IEnumerable<CodeInstruction>)ARHarmonyPatches.GetMethod("BodyReferenceTranspiler").Invoke(null, new object[] { instructions });
        }
    }
}

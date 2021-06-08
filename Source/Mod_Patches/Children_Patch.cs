using HarmonyLib;
using System;
using System.Reflection;
using Verse;
using static HarmonyLib.AccessTools;
using static RimThreaded.RimThreadedHarmony;

namespace RimThreaded.Mod_Patches
{
    class Children_Patch
    {
        public static Type childrenHarmonyHediffComp_Discoverable_CheckDiscovered_Patch;
        public static void Patch()
        {
            childrenHarmonyHediffComp_Discoverable_CheckDiscovered_Patch = TypeByName("Children.ChildrenHarmony+HediffComp_Discoverable_CheckDiscovered_Patch");
            Type patched;
            if (childrenHarmonyHediffComp_Discoverable_CheckDiscovered_Patch != null)
            {
                string methodName = "CheckDiscovered_Pre";
                patched = typeof(childrenHarmonyHediffComp_Discoverable_CheckDiscovered_Patch_Transpile);
                Log.Message("RimThreaded is patching " + childrenHarmonyHediffComp_Discoverable_CheckDiscovered_Patch.FullName + " " + methodName);
                Transpile(childrenHarmonyHediffComp_Discoverable_CheckDiscovered_Patch, patched, methodName);
                PawnComponentsUtility_Patch.RunDestructivePatches();
            }

        }
    }
}

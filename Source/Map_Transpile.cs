using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Verse;

namespace RimThreaded
{
    public class Map_Transpile
    {
        public static Dictionary<Map, AutoResetEvent> skyManagerStartEvents = new Dictionary<Map, AutoResetEvent>();

        internal static void RunNonDestructivePatches()
        {
            Type original = typeof(Map);
            Type patched = typeof(Map_Transpile);
            RimThreadedHarmony.Transpile(original, patched, "MapUpdate");
        }
        public static IEnumerable<CodeInstruction> MapUpdate(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            List<CodeInstruction> instructionsList = instructions.ToList();
            int i = 0;
            while (i < instructionsList.Count)
            {
                // skyManager.SkyManagerUpdate();
                //IL_0005: ldarg.0
                //IL_0006: ldfld class Verse.SkyManager Verse.Map::skyManager
                //IL_000b: callvirt instance void Verse.SkyManager::SkyManagerUpdate()
                if (i+2 < instructionsList.Count &&
                    instructionsList[i].opcode == OpCodes.Ldarg_0 &&
                    instructionsList[i + 1].opcode == OpCodes.Ldfld && (FieldInfo)(instructionsList[i + 1].operand) == AccessTools.Field(typeof(Map), "skyManager") &&
                    instructionsList[i + 2].opcode == OpCodes.Callvirt && (MethodInfo)instructionsList[i + 2].operand == AccessTools.Method(typeof(Map), "SkyManagerUpdate")
                    )
                {
                    // SkyManagerUpdate2(__instance);
                    //IL_0005: ldarg.0
                    //IL_0006: call void RimThreaded.Map_Patch::SkyManagerUpdate2(class ['Assembly-CSharp'] Verse.Map)
                    
                    //yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(Map_Patch), "skyManagerStartEvents"));
                    yield return new CodeInstruction(OpCodes.Ldarg_0); //this
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Map_Transpile), "SkyManagerUpdate2"));
                    i += 2;
                }
                else
                {
                    yield return instructionsList[i];
                }
                i++;
            }            
        }

        public static void SkyManagerUpdate2(Map __instance)
        {
            if (!skyManagerStartEvents.TryGetValue(__instance, out AutoResetEvent skyManagerStartEvent))
            {
                skyManagerStartEvent = new AutoResetEvent(false);
                skyManagerStartEvents.Add(__instance, skyManagerStartEvent);
                new Thread(() =>
                {
                    AutoResetEvent skyManagerStartEvent2 = skyManagerStartEvents[__instance];
                    SkyManager skyManager = __instance.skyManager;
                    while (true)
                    {
                        skyManagerStartEvent2.WaitOne();
                        skyManager.SkyManagerUpdate();
                    }
                }){IsBackground = true}.Start();
            }
            skyManagerStartEvent.Set();
        }

    }
}

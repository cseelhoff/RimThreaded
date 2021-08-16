using HarmonyLib;
using System;
using System.Reflection;
using Verse;
using System.Collections.Generic;
using System.Reflection.Emit;
using static HarmonyLib.AccessTools;
using static RimThreaded.RimThreadedHarmony;

namespace RimThreaded.Mod_Patches
{
    class VEE_Patch
    {
        public static void Patch()
        {
            Type VEE_FertilityGrid = TypeByName("VEE.FertilityGrid_Patch");
            if (VEE_FertilityGrid != null)
            {
                string methodName = nameof(Postfix);
                Log.Message("RimThreaded is patching " + VEE_FertilityGrid.FullName + " " + methodName);
                Transpile(VEE_FertilityGrid, typeof(VEE_Patch), methodName);
            }
            Type VEE_Plant_GrowthRate = TypeByName("VEE.Plant_GrowthRate_Patch");
            if (VEE_Plant_GrowthRate != null)
            {
                string methodName = nameof(Postfix);
                Log.Message("RimThreaded is patching " + VEE_Plant_GrowthRate.FullName + " " + methodName);
                Transpile(VEE_Plant_GrowthRate, typeof(VEE_Patch), methodName);
            }
            Type VEE_Plant_TickLong = TypeByName("VEE.Plant_TickLong_Patch");
            if (VEE_Plant_TickLong != null)
            {
                string methodName = nameof(Postfix);
                Log.Message("RimThreaded is patching " + VEE_Plant_TickLong.FullName + " " + methodName);
                Transpile(VEE_Plant_TickLong, typeof(VEE_Patch), methodName);
            }
        }
        
        public static bool ContainsKey(Dictionary<Map, object> MapComp_Drought, Map m)
        {
            lock (MapComp_Drought)
            {
                return MapComp_Drought.ContainsKey(m);
            }
        }
        public static void Add(Dictionary<Map, object> MapComp_Drought, Map m, object j)
        {
            lock (MapComp_Drought)
            {
                MapComp_Drought[m] = j;
            }
        }
        public static bool TryGetValue(Dictionary<Map, object> MapComp_Drought, Map m, out object j)
        {
            lock (MapComp_Drought)
            {
                return MapComp_Drought.TryGetValue(m, out j);
            }
        }

        public static IEnumerable<CodeInstruction> Postfix(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            Type MapComp_Drought = typeof(Dictionary<,>).MakeGenericType(new[] { typeof(Map), TypeByName("VEE.MapComp_Drought") });
            foreach (CodeInstruction i in instructions)
            {
                if (i.opcode == OpCodes.Callvirt || i.opcode == OpCodes.Call)
                {
                    if ((MethodInfo)i.operand == Method(MapComp_Drought, "ContainsKey"))
                    {
                        i.operand = Method(typeof(VEE_Patch), nameof(ContainsKey));
                    }
                    if ((MethodInfo)i.operand == Method(MapComp_Drought, "Add"))
                    {
                        i.operand = Method(typeof(VEE_Patch), nameof(Add));
                    }
                    if ((MethodInfo)i.operand == Method(MapComp_Drought, "TryGetValue"))
                    {
                        i.operand = Method(typeof(VEE_Patch), nameof(TryGetValue));
                    }
                }
                yield return i;
            }
        }
    }
}

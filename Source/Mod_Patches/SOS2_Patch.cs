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
    class SOS2_Patch
    {
        public static void Patch()
        {
            Type ShipInteriorMod2 = TypeByName("SaveOurShip2.ShipInteriorMod2");
            if (ShipInteriorMod2 != null)
            {
                string methodName = nameof(hasSpaceSuit);
                Log.Message("RimThreaded is patching " + ShipInteriorMod2.FullName + " " + methodName);
                Transpile(ShipInteriorMod2, typeof(SOS2_Patch), methodName);
            }
            Type ApparelTracker_Notify_Added = TypeByName("SaveOurShip2.ShipInteriorMod2+ApparelTracker_Notify_Added");//+for nested classes
            if (ApparelTracker_Notify_Added != null)
            {
                string methodName = nameof(Postfix);
                Log.Message("RimThreaded is patching " + ApparelTracker_Notify_Added.FullName + " " + methodName);
                Transpile(ApparelTracker_Notify_Added, typeof(SOS2_Patch), methodName);
            }
            Type ApparelTracker_Notify_Removed = TypeByName("SaveOurShip2.ShipInteriorMod2+ApparelTracker_Notify_Removed");//+for nested classes
            if (ApparelTracker_Notify_Removed != null)
            {
                string methodName = nameof(Postfix);
                Log.Message("RimThreaded is patching " + ApparelTracker_Notify_Removed.FullName + " " + methodName);
                Transpile(ApparelTracker_Notify_Removed, typeof(SOS2_Patch), methodName);
            }
        }

        public static void Add(Dictionary<int, Tuple<int, bool>> _cache_spacesuit, int i, Tuple<int, bool> t)
        {
            lock (_cache_spacesuit)
            {
                _cache_spacesuit[i] = t;
            }
        }
        public static bool TryGetValue(Dictionary<int, Tuple<int, bool>> _cache_spacesuit, int i, out Tuple<int, bool> t)
        {
            lock (_cache_spacesuit)
            {
                return _cache_spacesuit.TryGetValue(i, out t);
            }
        }
        public static int RemoveAll(Dictionary<int, Tuple<int, bool>> _cache_spacesuit, Predicate<KeyValuePair<int, Tuple<int,bool>>> p)
        {
            lock (_cache_spacesuit)
            {
                return _cache_spacesuit.RemoveAll(p);
            }
        }

        public static IEnumerable<CodeInstruction> hasSpaceSuit(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            Type _cache_spacesuit = typeof(Dictionary<int, Tuple<int, bool>>);
            foreach (CodeInstruction i in instructions)
            {
                if (i.opcode == OpCodes.Callvirt)
                {
                    if ((MethodInfo)i.operand == Method(_cache_spacesuit, "set_Item"))
                    {
                        i.operand = Method(typeof(SOS2_Patch), nameof(Add));
                    }
                    if ((MethodInfo)i.operand == Method(_cache_spacesuit, "TryGetValue"))
                    {
                        i.operand = Method(typeof(SOS2_Patch), nameof(TryGetValue));
                    }
                }
                yield return i;
            }
        }

        public static IEnumerable<CodeInstruction> Postfix(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            Type _cache_spacesuit = typeof(Dictionary<int, Tuple<int, bool>>);
            foreach (CodeInstruction i in instructions)
            {
                if (i.opcode == OpCodes.Call)
                {
                    if ((MethodInfo)i.operand == Method(_cache_spacesuit, "RemoveAll"))
                    {
                        i.operand = Method(typeof(SOS2_Patch), nameof(RemoveAll));
                    }
                }
                yield return i;
            }
        }
    }
}

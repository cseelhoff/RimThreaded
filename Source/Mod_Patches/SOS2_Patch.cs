using HarmonyLib;
using System;
using System.Reflection;
using Verse;
using System.Collections.Generic;
using System.Reflection.Emit;
using static HarmonyLib.AccessTools;
using static RimThreaded.RimThreadedHarmony;
using System.Threading;
namespace RimThreaded.Mod_Patches
{
    class SOS2_Patch
    {
        public static ReaderWriterLockSlim ProjectilesLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
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

            // ------------- SHIP PROJECTILE LOCKS

            //SaveOurShip2.WorldSwitchUtility.ExposeData X
            //RimwWorld.ShipCombatManager.RegisterProjectile X
            //RimwWorld.ShipCombatManager.Tick X
            //RimwWorld.ShipCombatManager.StartBattle X
            //RimwWorld.WorldObjectOrbitingShip.ShouldRemoveMapNow X
            //SaveOurShip2.ShipCombatOnGUI.DrawShipRange Read X
            //RimwWorld.ShipCombatManager.IncomingTorpedoesInRange Read X

            Type PastWorldUWO2 = TypeByName("SaveOurShip2.PastWorldUWO2");
            if (PastWorldUWO2 != null)
            {
                string methodName = "ExposeData";
                Log.Message("RimThreaded is patching " + PastWorldUWO2.FullName + " " + methodName);
                MethodLocker.LockMethodOn(PastWorldUWO2, methodName, LockFlag.WriterLock, ProjectilesLock);
            }
            Type ShipCombatManager = TypeByName("RimWorld.ShipCombatManager");
            if (ShipCombatManager != null)
            {
                string methodName = "RegisterProjectile";
                Log.Message("RimThreaded is patching " + ShipCombatManager.FullName + " " + methodName);
                MethodLocker.LockMethodOn(ShipCombatManager, methodName, LockFlag.WriterLock, ProjectilesLock);

                methodName = "Tick";
                Log.Message("RimThreaded is patching " + ShipCombatManager.FullName + " " + methodName);
                MethodLocker.LockMethodOn(ShipCombatManager, methodName, LockFlag.WriterLock, ProjectilesLock);

                methodName = "StartBattle";
                Log.Message("RimThreaded is patching " + ShipCombatManager.FullName + " " + methodName);
                MethodLocker.LockMethodOn(ShipCombatManager, methodName, LockFlag.WriterLock, ProjectilesLock);

                methodName = "IncomingTorpedoesInRange";
                Log.Message("RimThreaded is patching " + ShipCombatManager.FullName + " " + methodName);
                MethodLocker.LockMethodOn(ShipCombatManager, methodName, LockFlag.ReaderLock, ProjectilesLock);
            }
            Type WorldObjectOrbitingShip = TypeByName("RimWorld.WorldObjectOrbitingShip");
            if (WorldObjectOrbitingShip != null)
            {
                string methodName = "ShouldRemoveMapNow";
                Log.Message("RimThreaded is patching " + WorldObjectOrbitingShip.FullName + " " + methodName);
                MethodLocker.LockMethodOn(WorldObjectOrbitingShip, methodName, LockFlag.WriterLock, ProjectilesLock);
            }
            Type ShipCombatOnGUI = TypeByName("SaveOurShip2.ShipCombatOnGUI");
            if (ShipCombatOnGUI != null)
            {
                string methodName = "DrawShipRange";
                Log.Message("RimThreaded is patching " + ShipCombatOnGUI.FullName + " " + methodName);
                MethodLocker.LockMethodOn(ShipCombatOnGUI, methodName, LockFlag.ReaderLock, ProjectilesLock);
            }
            // ---------- END SHIP PROJECTILE LOCKS
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
            Type GenCollection = typeof(Verse.GenCollection);
            foreach (CodeInstruction i in instructions)
            {
                if (i.opcode == OpCodes.Call)
                {
                    if ((MethodInfo)i.operand == Method(GenCollection, "RemoveAll", new Type[] { typeof(Dictionary<int, Tuple<int, bool>>), typeof(Predicate<KeyValuePair<int, Tuple<int, bool>>>) }, new Type[] { typeof(int), typeof(Tuple<int, bool>) }))
                    {
                        i.operand = Method(typeof(SOS2_Patch), nameof(RemoveAll));
                    }
                }
                yield return i;
            }
        }
    }
}

using System;
using Verse;
using static HarmonyLib.AccessTools;
using RimWorld;

namespace RimThreaded.Mod_Patches
{
    class Fluffy_Breakdowns_Patch
    {
        public static void Patch()
        {
            Type MapComponent_Durability = TypeByName("Fluffy_Breakdowns.MapComponent_Durability");//Fluffy_Breakdowns.MapComponent_Durability.GetDurability
            if (MapComponent_Durability != null)
            {
                string methodName = "GetDurability";
                Log.Message("RimThreaded is patching " + MapComponent_Durability.FullName + " " + methodName);
                MethodLocker.LockMethodOnInstance(MapComponent_Durability, methodName, LockFlag.WriterLock, OTypes: new Type[] { typeof(CompBreakdownable) });
                methodName = "ExposeData";
                Log.Message("RimThreaded is patching " + MapComponent_Durability.FullName + " " + methodName);
                MethodLocker.LockMethodOnInstance(MapComponent_Durability, methodName, LockFlag.WriterLock);
                methodName = "MapComponentTick";
                Log.Message("RimThreaded is patching " + MapComponent_Durability.FullName + " " + methodName);
                MethodLocker.LockMethodOnInstance(MapComponent_Durability, methodName, LockFlag.WriterLock);
                methodName = "SetDurability";
                Log.Message("RimThreaded is patching " + MapComponent_Durability.FullName + " " + methodName);
                MethodLocker.LockMethodOnInstance(MapComponent_Durability, methodName, LockFlag.WriterLock, OTypes: new Type[] { typeof(CompBreakdownable), typeof(float) });
            }
        }
    }
}

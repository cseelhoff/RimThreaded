
using System;
using Verse;
using static HarmonyLib.AccessTools;
using System.Threading;

namespace RimThreaded.Mod_Patches
{
    public class TurnItOnAndOff_Patch
    {
        public static ReaderWriterLockSlim buildingsInUseThisTickLock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);//maybe not
        public static void Patch()
        {
            Type TurnItOnandOff = TypeByName("TurnOnOffRePowered.TurnItOnandOff");
            if (TurnItOnandOff != null)
            {
                //----------- buildingsInUseThisTick
                string methodName = "EvaluateRimfactoryWork";
                Log.Message("RimThreaded is patching " + TurnItOnandOff.FullName + " " + methodName);
                MethodLocker.LockMethodOn(TurnItOnandOff, methodName, LockFlag.WriterLock, buildingsInUseThisTickLock);
                methodName = "Tick";
                Log.Message("RimThreaded is patching " + TurnItOnandOff.FullName + " " + methodName);
                MethodLocker.LockMethodOn(TurnItOnandOff, methodName, LockFlag.WriterLock, buildingsInUseThisTickLock);
                methodName = "ClearVariables";
                Log.Message("RimThreaded is patching " + TurnItOnandOff.FullName + " " + methodName);
                MethodLocker.LockMethodOn(TurnItOnandOff, methodName, LockFlag.WriterLock, buildingsInUseThisTickLock);
                methodName = "EvalTurrets";
                Log.Message("RimThreaded is patching " + TurnItOnandOff.FullName + " " + methodName);
                MethodLocker.LockMethodOn(TurnItOnandOff, methodName, LockFlag.WriterLock, buildingsInUseThisTickLock);
                methodName = "EvalResearchTables";
                Log.Message("RimThreaded is patching " + TurnItOnandOff.FullName + " " + methodName);
                MethodLocker.LockMethodOn(TurnItOnandOff, methodName, LockFlag.WriterLock, buildingsInUseThisTickLock);
                methodName = "EvalBeds";
                Log.Message("RimThreaded is patching " + TurnItOnandOff.FullName + " " + methodName);
                MethodLocker.LockMethodOn(TurnItOnandOff, methodName, LockFlag.WriterLock, buildingsInUseThisTickLock);
                methodName = "EvalDeepDrills";
                Log.Message("RimThreaded is patching " + TurnItOnandOff.FullName + " " + methodName);
                MethodLocker.LockMethodOn(TurnItOnandOff, methodName, LockFlag.WriterLock, buildingsInUseThisTickLock);
                methodName = "AddBuildingUsed";
                Log.Message("RimThreaded is patching " + TurnItOnandOff.FullName + " " + methodName);
                MethodLocker.LockMethodOn(TurnItOnandOff, methodName, LockFlag.WriterLock, buildingsInUseThisTickLock);
                methodName = "EvalScheduledBuildings";
                Log.Message("RimThreaded is patching " + TurnItOnandOff.FullName + " " + methodName);
                MethodLocker.LockMethodOn(TurnItOnandOff, methodName, LockFlag.WriterLock, buildingsInUseThisTickLock);
                methodName = "EvalHydroponicsBasins";
                Log.Message("RimThreaded is patching " + TurnItOnandOff.FullName + " " + methodName);
                MethodLocker.LockMethodOn(TurnItOnandOff, methodName, LockFlag.WriterLock, buildingsInUseThisTickLock);
                methodName = "EvalAutodoors";
                Log.Message("RimThreaded is patching " + TurnItOnandOff.FullName + " " + methodName);
                MethodLocker.LockMethodOn(TurnItOnandOff, methodName, LockFlag.WriterLock, buildingsInUseThisTickLock);
                methodName = "PowerFactor";
                Log.Message("RimThreaded is patching " + TurnItOnandOff.FullName + " " + methodName);
                MethodLocker.LockMethodOn(TurnItOnandOff, methodName, LockFlag.ReaderLock, buildingsInUseThisTickLock);
            }
        }
    }
}

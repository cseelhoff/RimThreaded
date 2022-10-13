using HarmonyLib;
using System;
using System.Collections.Generic;
using Verse;
using System.Reflection.Emit;
using System.Linq;
using static Verse.PawnCapacitiesHandler;

namespace RimThreaded.RW_Patches
{

    public class PawnCapacitiesHandler_Patch
    {

        static readonly Type original = typeof(PawnCapacitiesHandler);
        static readonly Type patched = typeof(PawnCapacitiesHandler_Patch);
        internal static void RunDestructivePatches()
        {
            RimThreadedHarmony.Prefix(original, patched, "GetLevel");
        }
        internal static void RunNonDestructivePatches()
        {
            //RimThreadedHarmony.Transpile(original, patched, "GetLevel"); //wrap method in lock to protect CacheStatus changes
            RimThreadedHarmony.Transpile(original, patched, "Notify_CapacityLevelsDirty"); //wrap method in lock to protect CacheStatus changes
        }

        public static IEnumerable<CodeInstruction> GetLevel2(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            List<CodeInstruction> instructionsList = instructions.ToList();
            int i = 0;
            List<CodeInstruction> loadLockObjectInstructions = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Ldarg_0)
                };
            LocalBuilder lockObject = iLGenerator.DeclareLocal(typeof(PawnCapacitiesHandler));
            LocalBuilder lockTaken = iLGenerator.DeclareLocal(typeof(bool));
            foreach (CodeInstruction ci in RimThreadedHarmony.EnterLock(lockObject, lockTaken, loadLockObjectInstructions, instructionsList[i]))
                yield return ci;

            while (i < instructionsList.Count - 3)
            {
                yield return instructionsList[i++];
            }

            foreach (CodeInstruction ci in RimThreadedHarmony.ExitLock(iLGenerator, lockObject, lockTaken, instructionsList[i]))
                yield return ci;

            while (i < instructionsList.Count)
            {
                yield return instructionsList[i++];
            }
        }
        public static IEnumerable<CodeInstruction> Notify_CapacityLevelsDirty(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            List<CodeInstruction> instructionsList = instructions.ToList();
            int i = 0;
            List<CodeInstruction> loadLockObjectInstructions = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Ldarg_0)
                };
            LocalBuilder lockObject = iLGenerator.DeclareLocal(typeof(PawnCapacitiesHandler));
            LocalBuilder lockTaken = iLGenerator.DeclareLocal(typeof(bool));
            foreach (CodeInstruction ci in RimThreadedHarmony.EnterLock(lockObject, lockTaken, loadLockObjectInstructions, instructionsList[i]))
                yield return ci;

            while (i < instructionsList.Count - 1)
            {
                yield return instructionsList[i++];
            }

            foreach (CodeInstruction ci in RimThreadedHarmony.ExitLock(iLGenerator, lockObject, lockTaken, instructionsList[i]))
                yield return ci;

            yield return instructionsList[i];
        }


        public static bool GetLevel(PawnCapacitiesHandler __instance, ref float __result, PawnCapacityDef capacity)
        {
            if (__instance.pawn.health.Dead)
            {
                __result = 0f;
                return false;
            }
            if (__instance.cachedCapacityLevels == null)
            {
                __instance.Notify_CapacityLevelsDirty();
            }
            lock (__instance)
            {
                CacheElement cacheElement = __instance.cachedCapacityLevels[capacity];
                if (cacheElement.status == CacheStatus.Caching)
                {
                    Log.Error($"Detected infinite stat recursion when evaluating {capacity}");
                    __result = 0f;
                    return false;
                }
                if (cacheElement.status == CacheStatus.Uncached)
                {
                    cacheElement.status = CacheStatus.Caching;
                    try
                    {
                        cacheElement.value = PawnCapacityUtility.CalculateCapacityLevel(__instance.pawn.health.hediffSet, capacity);
                    }
                    finally
                    {
                        cacheElement.status = CacheStatus.Cached;
                    }
                }
                __result = cacheElement.value;
                return false;
            }
        }

        //public void Notify_CapacityLevelsDirty2(PawnCapacitiesHandler __instance)
        //{
        //    lock (__instance)
        //    {
        //        if (__instance.cachedCapacityLevels == null)
        //        {
        //            __instance.cachedCapacityLevels = new DefMap<PawnCapacityDef, CacheElement>();
        //        }
        //        for (int i = 0; i < __instance.cachedCapacityLevels.Count; i++)
        //        {
        //            __instance.cachedCapacityLevels[i].status = CacheStatus.Uncached;
        //        }
        //    }
        //}

    }
}

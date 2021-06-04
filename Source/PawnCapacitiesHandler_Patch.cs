using HarmonyLib;
using System;
using System.Collections.Generic;
using Verse;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{

    public class PawnCapacitiesHandler_Patch
    {

        static Type original = typeof(PawnCapacitiesHandler);
        static Type patched = typeof(PawnCapacitiesHandler_Patch);
        internal static void RunNonDestructivePatches()
        {
            RimThreadedHarmony.Transpile(original, patched, "GetLevel");
        }

        internal static void RunDestructivePatches()
        {
            RimThreadedHarmony.Prefix(original, patched, "Notify_CapacityLevelsDirty");
            RimThreadedHarmony.Prefix(original, patched, "Clear");
            RimThreadedHarmony.Prefix(original, patched, "CapableOf");
            ConstructorInfo constructorMethod = original.GetConstructor(new Type[] { typeof(Pawn) });
            MethodInfo cpMethod = patched.GetMethod("Postfix_Constructor");
            RimThreadedHarmony.harmony.Patch(constructorMethod, postfix: new HarmonyMethod(cpMethod));
        }
        
        public static Dictionary<PawnCapacitiesHandler, DefMap<PawnCapacityDef, CacheElement2>> cachedCapacityLevelsDict =
            new Dictionary<PawnCapacitiesHandler, DefMap<PawnCapacityDef, CacheElement2>>();
        
            public class CacheElement2        
        {
            public CacheStatus status;

            public float value;
        }
        public enum CacheStatus
        {
            Uncached,
            Caching,
            Cached
        }

        public static IEnumerable<CodeInstruction> GetLevel(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            int[] matchesFound = new int[1]; //EDIT
            List<CodeInstruction> instructionsList = instructions.ToList();
            LocalBuilder cacheElement = iLGenerator.DeclareLocal(typeof(CacheElement2));
            int i = 0;
            while (i < instructionsList.Count)
            {
                int matchIndex = 0;
                if (
                    i + 2 < instructionsList.Count &&
                    instructionsList[i + 1].opcode == OpCodes.Ldfld &&
                    (FieldInfo)instructionsList[i + 1].operand == Field(typeof(PawnCapacitiesHandler), "cachedCapacityLevels") &&
                    instructionsList[i + 2].opcode == OpCodes.Brtrue_S
                    )
                {
                    instructionsList[i].opcode = OpCodes.Ldarg_0;
                    instructionsList[i].operand = null;
                    yield return instructionsList[i++];
                    yield return new CodeInstruction(OpCodes.Ldarg_1);
                    yield return new CodeInstruction(OpCodes.Call, Method(typeof(PawnCapacitiesHandler_Patch), "getCacheElementResult"));
                    yield return new CodeInstruction(OpCodes.Ret);
                    matchesFound[matchIndex]++;
                    break;
                }
                yield return instructionsList[i++];
            }
            for (int mIndex = 0; mIndex < matchesFound.Length; mIndex++)
            {
                if (matchesFound[mIndex] < 1)
                    Log.Error("IL code instruction set " + mIndex + " not found");
            }
        }

        public static void Postfix_Constructor(PawnCapacitiesHandler __instance, Pawn pawn)
        {
            cachedCapacityLevelsDict[__instance] = new DefMap<PawnCapacityDef, CacheElement2>();
        }
        public static bool Clear(PawnCapacitiesHandler __instance)
        {
            cachedCapacityLevelsDict[__instance] = null;
            return false;
        }
        public static bool Notify_CapacityLevelsDirty(PawnCapacitiesHandler __instance)
        {
            if (cachedCapacityLevelsDict[__instance] == null)
            {
                cachedCapacityLevelsDict[__instance] = new DefMap<PawnCapacityDef, CacheElement2>();
            }

            for (int i = 0; i < cachedCapacityLevelsDict[__instance].Count; i++)
            {
                cachedCapacityLevelsDict[__instance][i].status = CacheStatus.Uncached;
            }
            return false;
        }
        
        public static bool CapableOf(PawnCapacitiesHandler __instance, ref bool __result, PawnCapacityDef capacity)
        {
            float levelResult = 0f;
            __result = false;
            if (capacity != null)
            {
                GetLevel(__instance, ref levelResult, capacity);
                __result = levelResult > capacity.minForCapable;
            }
            return false;
        }
        
        public static bool GetLevel(PawnCapacitiesHandler __instance, ref float __result, PawnCapacityDef capacity)
        {
            if (__instance.pawn.health.Dead)
            {
                __result = 0f;
                return false;
            }
            //if (cachedCapacityLevels == null) //REMOVED
            //CacheElement cacheElement = cachedCapacityLevels[capacity]; //REMOVED   
            
            __result = getCacheElementResult(__instance, capacity);
            return false;
        }

        private static float getCacheElementResult(PawnCapacitiesHandler __instance, PawnCapacityDef capacity)
        {
            if (capacity == null) //ADDED
            {
                return 0f;
            }
            CacheElement2 cacheElement = get_cacheElement(__instance, capacity); //ADDED
            lock (cacheElement) //ADDED
            {
                if (cacheElement.status == CacheStatus.Caching)
                {
                    Log.Error($"Detected infinite stat recursion when evaluating {capacity}");
                    return 0f;
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
            }
            return cacheElement.value;
        }

        private static CacheElement2 get_cacheElement(PawnCapacitiesHandler __instance, PawnCapacityDef capacity)
        {
            DefMap<PawnCapacityDef, CacheElement2> defMap = cachedCapacityLevelsDict[__instance];
            if (defMap == null)
            {
                defMap = new DefMap<PawnCapacityDef, CacheElement2>();
                cachedCapacityLevelsDict[__instance] = defMap;
                for (int i = 0; i < defMap.Count; i++)
                {
                    defMap[i].status = CacheStatus.Uncached;
                }
            }
            return defMap[capacity];
        }


    }
}

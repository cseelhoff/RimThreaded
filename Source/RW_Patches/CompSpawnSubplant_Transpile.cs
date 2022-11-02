using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using HarmonyLib;
using RimWorld;
using ThreadSafeLinkedList;
using Verse;
using static HarmonyLib.AccessTools;

namespace RimThreaded.RW_Patches
{

    public class CompSpawnSubplant_Transpile
    {
        //static Dictionary<CompSpawnSubplant, ThreadSafeLinkedList<Thing>> subplants;
        internal static void RunNonDestructivePatches()
        {
            //subplants = new Dictionary<CompSpawnSubplant, ThreadSafeLinkedList<Thing>>();
            //Type original = typeof(CompSpawnSubplant);
            //Type patched = typeof(CompSpawnSubplant_Transpile);
            //RimThreadedHarmony.harmony.Patch(Constructor(original), transpiler: new HarmonyMethod(Method(patched, nameof(CreateNewThreadSafeLinkedList_Thing))));
            //RimThreadedHarmony.harmony.Patch(Method(original, "get_SubplantsForReading"), transpiler: new HarmonyMethod(Method(patched, nameof(ConvertListThing_ToList))));
            //RimThreadedHarmony.harmony.Patch(Method(original, nameof(CompSpawnSubplant.DoGrowSubplant)), transpiler: new HarmonyMethod(Method(patched, nameof(ReplaceAdd_Thing))));
            //RimThreadedHarmony.harmony.Patch(Method(original, nameof(CompSpawnSubplant.Cleanup)), transpiler: new HarmonyMethod(Method(patched, nameof(ReplaceRemoveAll_Thing))));
            //RimThreadedHarmony.harmony.Patch(Method(original, nameof(CompSpawnSubplant.PostExposeData)), transpiler: new HarmonyMethod(Method(patched, nameof(ReplaceRemoveAll_Thing))));
            
            //RimThreadedHarmony.harmony.Patch(Method(original, nameof(CompSpawnSubplant.PostExposeData)), transpiler: new HarmonyMethod(Method(patched, nameof(ReplaceLook_Thing)))); //1.4
        }
        internal static void RunDestructivePatches()
        {
            Type original = typeof(CompSpawnSubplant);
            Type patched = typeof(CompSpawnSubplant_Transpile);
            RimThreadedHarmony.Prefix(original, patched, nameof(AddProgress));
            RimThreadedHarmony.Prefix(original, patched, nameof(TryGrowSubplants));
            //RimThreadedHarmony.Prefix(original, patched, nameof(get_SubplantsForReading));
            //RimThreadedHarmony.Prefix(original, patched, nameof(Cleanup));

        }


        //public List<Thing> SubplantsForReading
        //{
        //    get
        //    {
        //        Cleanup();
        //        return subplants;    -----REPLACE WITH subplants.ToList()
        //    }
        //}
        /*
        public static bool get_SubplantsForReading(CompSpawnSubplant __instance, ref List<Thing> __result)
        {
            __instance.Cleanup();
            //__result = __instance.subplants;
            
            return false;
        }
        */
        public static IEnumerable<CodeInstruction> ConvertListThing_ToList(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            foreach (CodeInstruction codeInstruction in instructions)
            {
                yield return codeInstruction;
                if (codeInstruction.opcode == OpCodes.Ldfld && ((FieldInfo)codeInstruction.operand).FieldType == typeof(List<Thing>))
                {
                    yield return new CodeInstruction(OpCodes.Call, Method(typeof(ThreadSafeLinkedList<Thing>), nameof(ThreadSafeLinkedList<Thing>.ToList)));
                }
            }
        }

        //REPLACE:
        //private List<Thing> subplants = new List<Thing>();
        //WITH:
        //private List<Thing> subplants = new ThreadSafeLinkedList<Thing>();
        public static IEnumerable<CodeInstruction> CreateNewThreadSafeLinkedList_Thing(IEnumerable<CodeInstruction> instructions, ILGenerator _)
        {
            foreach (CodeInstruction codeInstruction in instructions)
            {
                if (codeInstruction.opcode == OpCodes.Newobj && (ConstructorInfo)codeInstruction.operand == Constructor(typeof(List<Thing>), Type.EmptyTypes))
                {
                    codeInstruction.operand = Constructor(typeof(ThreadSafeLinkedList<Thing>));
                }
                yield return codeInstruction;
            }
        }


        //public void Cleanup()
        //{
        //    subplants.RemoveAll((Thing p) => !p.Spawned);   -----REPLACE WITH ThreadSafeLinkedList.RemoveAll(predicate)
        //}
        public static bool Cleanup(CompSpawnSubplant __instance)
        {
            __instance.subplants.RemoveAll((Thing p) => !p.Spawned);
            return false;
        }
        public static IEnumerable<CodeInstruction> ReplaceRemoveAll_Thing(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            foreach (CodeInstruction codeInstruction in instructions)
            {
                if (codeInstruction.opcode == OpCodes.Callvirt && (MethodInfo)codeInstruction.operand == Method(typeof(List<Thing>), nameof(List<Thing>.RemoveAll)))
                {
                    codeInstruction.operand = Method(typeof(ThreadSafeLinkedList<Thing>), nameof(ThreadSafeLinkedList<Thing>.RemoveAll));
                }
                yield return codeInstruction;
            }
        }

        //public void DoGrowSubplant()
        //subplants.Add(GenSpawn.Spawn(Props.subplant, intVec, parent.Map));   ------REPLACE WITH ThreadSafeLinkedList.Add(object)
        public static IEnumerable<CodeInstruction> ReplaceAdd_Thing(IEnumerable<CodeInstruction> instructions, ILGenerator iLGenerator)
        {
            foreach (CodeInstruction codeInstruction in instructions)
            {
                if (codeInstruction.opcode == OpCodes.Callvirt && (MethodInfo)codeInstruction.operand == Method(typeof(List<Thing>), nameof(List<Thing>.Add)))
                {
                    codeInstruction.operand = Method(typeof(ThreadSafeLinkedList<Thing>), nameof(ThreadSafeLinkedList<Thing>.Add));
                }
                yield return codeInstruction;
            }
        }

        //public override void PostExposeData()
        //Scribe_Collections.Look(ref subplants, "subplants", LookMode.Reference);   ------Add Scribe_Collections.Look(ref ThreadSafeLinkedList, string, LookMode)
        public static IEnumerable<CodeInstruction> ReplaceLook_Thing(IEnumerable<CodeInstruction> instructions, ILGenerator _)
        {
            foreach (CodeInstruction codeInstruction in instructions)
            {
                if (codeInstruction.opcode == OpCodes.Call && (MethodInfo)codeInstruction.operand == Method(typeof(Scribe_Collections), "Look", new Type[] { typeof(List<>).MakeByRefType(), typeof(string), typeof(LookMode), typeof(object[]) }, new Type[] { typeof(Thing) }))
                {
                    codeInstruction.operand = Method(typeof(Scribe_Collections_Patch), "Look1", null, new Type[] { typeof(Thing) });
                }
                yield return codeInstruction;
            }
        }
        public static bool AddProgress(CompSpawnSubplant __instance, float progress, bool ignoreMultiplier = false)
        {
            if (ModLister.CheckRoyalty("Subplant spawning")) { 
                if (!ignoreMultiplier)
                    progress *= __instance.ProgressMultiplier;
                progress *= (1f + __instance.parent.GetStatValue(StatDefOf.MeditationPlantGrowthOffset));
                lock (__instance) //threadsafe add for float
                {
                    __instance.progressToNextSubplant += progress;
                }

                Interlocked.Increment(ref __instance.meditationTicksToday); //used threadsafe increment
                __instance.TryGrowSubplants();
            }

            return false;
        }
        public static bool TryGrowSubplants(CompSpawnSubplant __instance)
        {
            while (__instance.progressToNextSubplant >= 1f)
            {
                bool grow = false;
                lock (__instance) //threadsafe subrtract for float
                {
                    if (__instance.progressToNextSubplant >= 1f)
                    {
                        __instance.progressToNextSubplant -= 1f;
                        grow = true;
                    }
                }
                if (grow)
                    __instance.DoGrowSubplant();
            }
            return false;
        }

    }
}

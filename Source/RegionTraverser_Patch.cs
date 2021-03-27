using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{

    public class RegionTraverser_Patch
    {
        [ThreadStatic] public static Queue<RegionTraverser.BFSWorker> freeWorkers;
        [ThreadStatic] public static int NumWorkers;

        public static void InitializeThreadStatics() //not sure why this is neccessary
        {
            freeWorkers = new Queue<RegionTraverser.BFSWorker>();
            NumWorkers = 8;
        }

        public static void RunNonDestructivePatches()
        {
            Type original = typeof(RegionTraverser);
            Type patched = typeof(RegionTraverser_Patch);
            //RimThreadedHarmony.AddAllMatchingFields(original, patched);
            RimThreadedHarmony.replaceFields.Add(Field(original, "NumWorkers"), Field(patched, "NumWorkers"));
            RimThreadedHarmony.replaceFields.Add(Field(original, "freeWorkers"), Field(patched, "freeWorkers"));
            RimThreadedHarmony.TranspileFieldReplacements(original, "BreadthFirstTraverse", new Type[] {
                typeof(Region),
                typeof(RegionEntryPredicate),
                typeof(RegionProcessor),
                typeof(int),
                typeof(RegionType)
            });
            RimThreadedHarmony.TranspileFieldReplacements(original, "RecreateWorkers");
            //ConstructorInfo constructorInfo = Constructor(original); // not sure why this doesn't work
            ConstructorInfo constructorInfo = ((ConstructorInfo[])((TypeInfo)original).DeclaredConstructors)[0];
            Log.Message(constructorInfo.ToString());
            RimThreadedHarmony.harmony.Patch(constructorInfo, transpiler: RimThreadedHarmony.replaceFieldsHarmonyTranspiler);
        }
    }
    
}

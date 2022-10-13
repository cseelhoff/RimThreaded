using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace RimThreaded.RW_Patches
{

    public class ThinkNode_SubtreesByTag_Patch
    {
        internal static void RunDestructivePatches()
        {
            Type original = typeof(ThinkNode_SubtreesByTag);
            Type patched = typeof(ThinkNode_SubtreesByTag_Patch);
            RimThreadedHarmony.Prefix(original, patched, "TryIssueJobPackage");
        }

        public static bool TryIssueJobPackage(ThinkNode_SubtreesByTag __instance, ref ThinkResult __result, Pawn pawn, JobIssueParams jobParams)
        {
            List<ThinkTreeDef> matchedTrees = new List<ThinkTreeDef>();
            foreach (ThinkTreeDef allDef in DefDatabase<ThinkTreeDef>.AllDefs)
            {
                if (allDef.insertTag == __instance.insertTag)
                {
                    matchedTrees.Add(allDef);
                }
            }
            matchedTrees = matchedTrees.OrderByDescending((tDef) => tDef.insertPriority).ToList();

            ThinkTreeDef thinkTreeDef;
            for (int i = 0; i < matchedTrees.Count; i++)
            {
                thinkTreeDef = matchedTrees[i];
                ThinkResult result = thinkTreeDef.thinkRoot.TryIssueJobPackage(pawn, jobParams);
                if (result.IsValid)
                {
                    __result = result;
                    return false;
                }
            }

            __result = ThinkResult.NoJob;
            return false;
        }

    }
}

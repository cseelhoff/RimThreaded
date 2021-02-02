using HarmonyLib;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Verse;
using Verse.AI;

namespace RimThreaded
{
    public class ThinkNode_PrioritySorter_Patch
    {
        [ThreadStatic]
        public static List<ThinkNode> workingNodes;

        public static bool TryIssueJobPackage(ThinkNode_PrioritySorter __instance, ref ThinkResult __result, Pawn pawn, JobIssueParams jobParams)
        {
            if(workingNodes == null)
            {
                workingNodes = new List<ThinkNode>();
            } else
            {
                workingNodes.Clear();
            }
            //List<ThinkNode> workingNodes = new List<ThinkNode>();
            int count = __instance.subNodes.Count;
            for (int i = 0; i < count; i++)
            {
                workingNodes.Insert(Rand.Range(0, workingNodes.Count - 1), __instance.subNodes[i]);
            }
            while (workingNodes.Count > 0)
            {
                float num1 = 0.0f;
                int index1 = -1;
                for (int index2 = 0; index2 < workingNodes.Count; ++index2)
                {
                    float num2 = 0.0f;
                    try
                    {
                        num2 = workingNodes[index2].GetPriority(pawn);
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Exception in " + __instance.GetType() + " GetPriority: " + ex.ToString(), false);
                    }
                    if (num2 > 0.0 && (double)num2 >= __instance.minPriority && num2 > num1)
                    {
                        num1 = num2;
                        index1 = index2;
                    }
                }
                if (index1 != -1)
                {
                    ThinkResult thinkResult = ThinkResult.NoJob;
                    try
                    {
                        thinkResult = workingNodes[index1].TryIssueJobPackage(pawn, jobParams);
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Exception in " + __instance.GetType() + " TryIssueJobPackage: " + ex.ToString(), false);
                    }
                    if (thinkResult.IsValid)
                    {
                        __result = thinkResult;
                        return false;
                    }
                    workingNodes.RemoveAt(index1);
                }
                else
                    break;
            }
            __result = ThinkResult.NoJob;
            return false;
		}
    }
    
}

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
    
    public class ThinkNode_Priority_Patch
    {

        public static bool TryIssueJobPackage(ThinkNode_Priority __instance, ref ThinkResult __result, Pawn pawn, JobIssueParams jobParams)
        {
            for (int index = 0; index < __instance.subNodes.Count; ++index )
            {
                try
                {
                    var subNode = __instance.subNodes[index];
                    if (subNode != null)
                    {
                        ThinkResult thinkResult = subNode.TryIssueJobPackage(pawn, jobParams);
                        if (thinkResult.IsValid)
                            __result = thinkResult;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("Exception in " + (object)__instance.GetType() + " TryIssueJobPackage: " + ex.ToString());
                }
            }

            __result = ThinkResult.NoJob;
            return false;
		}
    }
    
}

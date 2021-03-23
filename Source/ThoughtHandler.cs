using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;
using Verse;

namespace RimThreaded
{
    
    public class ThoughtHandler_Patch
    {
        
        public static bool MoodOffsetOfGroup(ThoughtHandler __instance, ref float __result, Thought group)
        {
            List<Thought> tmpThoughts = new List<Thought>();
            __instance.GetMoodThoughts(group, tmpThoughts);
            if (!tmpThoughts.Any())
            {
                __result = 0.0f;
                return false;
            }
            float num1 = 0.0f;
            float num2 = 1f;
            float num3 = 0.0f;
            for (int index = 0; index < tmpThoughts.Count; ++index)
            {
                Thought tmpThought = tmpThoughts[index];
                if(null != tmpThought) { 
                num1 += tmpThought.MoodOffset();
                num3 += num2;
                num2 *= tmpThought.def.stackedEffectMultiplier;
                    }
            }
            double num4 = num1 / (double)tmpThoughts.Count;
            tmpThoughts.Clear();
            double num5 = num3;
            __result = (float)(num4 * num5);
            return false;
        }

        public static bool TotalMoodOffset(ThoughtHandler __instance, ref float __result)
        {
            List<Thought> tmpThoughts = new List<Thought>();
            __instance.GetDistinctMoodThoughtGroups(tmpThoughts);
            float num = 0.0f;
            float moodOffset = 0f;
            for (int index = 0; index < tmpThoughts.Count; ++index) {
                MoodOffsetOfGroup(__instance, ref moodOffset, tmpThoughts[index]);
                num += moodOffset;
            }
            tmpThoughts.Clear();
            __result = num;
            return false;
        }

        public static bool OpinionOffsetOfGroup(ThoughtHandler __instance, ref int __result, ISocialThought group, Pawn otherPawn)
        {
            List<ISocialThought> tmpSocialThoughts = new List<ISocialThought>();
            __instance.GetSocialThoughts(otherPawn, group, tmpSocialThoughts);
            for (int index = tmpSocialThoughts.Count - 1; index >= 0; --index)
            {
                if (tmpSocialThoughts[index].OpinionOffset() == 0.0)
                    tmpSocialThoughts.RemoveAt(index);
            }
            if (!tmpSocialThoughts.Any())
            {
                __result = 0;
                return false;
            }
            ThoughtDef def = ((Thought)group).def;
            if (def.IsMemory && def.stackedEffectMultiplier != 1.0)
                tmpSocialThoughts.Sort((a, b) => ((Thought_Memory)a).age.CompareTo(((Thought_Memory)b).age));
            float f = 0.0f;
            float num = 1f;
            for (int index = 0; index < tmpSocialThoughts.Count; ++index)
            {
                f += tmpSocialThoughts[index].OpinionOffset() * num;
                num *= ((Thought)tmpSocialThoughts[index]).def.stackedEffectMultiplier;
            }
            tmpSocialThoughts.Clear();
            if (f == 0.0)
            {
                __result = 0;
                return false;
            }
            __result = f > 0.0 ? Mathf.Max(Mathf.RoundToInt(f), 1) : Mathf.Min(Mathf.RoundToInt(f), -1);
            return false;
        }

        public static bool TotalOpinionOffset(ThoughtHandler __instance, ref int __result, Pawn otherPawn)
        {
            List<ISocialThought> tmpTotalOpinionOffsetThoughts = new List<ISocialThought>();
            __instance.GetDistinctSocialThoughtGroups(otherPawn, tmpTotalOpinionOffsetThoughts);
            int num = 0;
            int opinionOffset = 0;
            for (int index = 0; index < tmpTotalOpinionOffsetThoughts.Count; ++index)
            {
                OpinionOffsetOfGroup(__instance, ref opinionOffset, tmpTotalOpinionOffsetThoughts[index], otherPawn);
                num += opinionOffset;
            }
            tmpTotalOpinionOffsetThoughts.Clear();
            __result = num;
            return false;
        }
        
    }

}

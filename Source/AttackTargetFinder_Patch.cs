using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace RimThreaded
{
    class AttackTargetFinder_Patch
    {
        [ThreadStatic] public static List<IAttackTarget> tmpTargets;
        [ThreadStatic] public static List<Pair<IAttackTarget, float>> availableShootingTargets;
        [ThreadStatic] public static List<float> tmpTargetScores;
        [ThreadStatic] public static List<bool> tmpCanShootAtTarget;
        [ThreadStatic] public static List<IntVec3> tempDestList;
        [ThreadStatic] public static List<IntVec3> tempSourceList;
        public static void InitializeThreadStatics()
        {
            tmpTargets = new List<IAttackTarget>();
            availableShootingTargets = new List<Pair<IAttackTarget, float>>();
            tmpTargetScores = new List<float>();
            tmpCanShootAtTarget = new List<bool>();
            tempDestList = new List<IntVec3>();
            tempSourceList = new List<IntVec3>();
        }

        internal static void RunNonDestructivePatches()
        {
            Type original = typeof(AttackTargetFinder);
            Type patched = typeof(AttackTargetFinder_Patch);
            RimThreadedHarmony.AddAllMatchingFields(original, patched);
            RimThreadedHarmony.TranspileFieldReplacements(original, "BestAttackTarget");
            RimThreadedHarmony.TranspileFieldReplacements(original, "GetAvailableShootingTargetsByScore");
            RimThreadedHarmony.TranspileFieldReplacements(original, "DebugDrawAttackTargetScores_Update");
            RimThreadedHarmony.TranspileFieldReplacements(original, "CanSee");
        }
    }
    


}
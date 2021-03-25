using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace RimThreaded
{
    class AttackTargetFinder_Patch
    {
        [ThreadStatic] public static List<IAttackTarget> tmpTargets = new List<IAttackTarget>();
        [ThreadStatic] public static List<Pair<IAttackTarget, float>> availableShootingTargets = new List<Pair<IAttackTarget, float>>();
        [ThreadStatic] public static List<float> tmpTargetScores = new List<float>();
        [ThreadStatic] public static List<bool> tmpCanShootAtTarget = new List<bool>();
        [ThreadStatic] public static List<IntVec3> tempDestList = new List<IntVec3>();
        [ThreadStatic] public static List<IntVec3> tempSourceList = new List<IntVec3>();
        public static void InitializeThreadStatics()
        {
            tmpTargets = new List<IAttackTarget>();
            availableShootingTargets = new List<Pair<IAttackTarget, float>>();
            tmpTargetScores = new List<float>();
            tmpCanShootAtTarget = new List<bool>();
            tempDestList = new List<IntVec3>();
            tempSourceList = new List<IntVec3>();
        }
    }
    


}
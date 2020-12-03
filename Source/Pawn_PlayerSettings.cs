using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{
    class Pawn_PlayerSettings_Patch
    {
        public static Dictionary<Pawn, List<Pawn>> servants = new Dictionary<Pawn, List<Pawn>>();
        //public static bool servantsFullyBuilt = false;
        public static FieldRef<Pawn_PlayerSettings, Pawn> master = FieldRefAccess<Pawn_PlayerSettings, Pawn>("master");
        public static FieldRef<Pawn_PlayerSettings, Pawn> pawn = FieldRefAccess<Pawn_PlayerSettings, Pawn>("pawn");
        public static bool set_Master(Pawn_PlayerSettings __instance, Pawn value)
		{
            if (master(__instance) == value)
            {
                return false;
            }

            if (value != null && !pawn(__instance).training.HasLearned(TrainableDefOf.Obedience))
            {
                Log.ErrorOnce("Attempted to set master for non-obedient pawn", 73908573);
                return false;
            }

            bool flag = ThinkNode_ConditionalShouldFollowMaster.ShouldFollowMaster(pawn(__instance));
            if (master(__instance) != null)
            {
                if (servants.TryGetValue(value, out List<Pawn> pawnList2))
                {
                    pawnList2.Remove(pawn(__instance));
                }
            }
            master(__instance) = value;
            if (!servants.TryGetValue(value, out List<Pawn> pawnList))
            {
                RebuildServants(value);
            }
            else
            {
                pawnList.Add(pawn(__instance));
            }

            if (pawn(__instance).Spawned && (flag || ThinkNode_ConditionalShouldFollowMaster.ShouldFollowMaster(pawn(__instance))))
            {
                pawn(__instance).jobs.EndCurrentJob(JobCondition.InterruptForced);
            }
            return false;
        }
        public static List<Pawn> RebuildServants(Pawn master)
        {
            List<Pawn> pawnList = new List<Pawn>();
            servants[master] = pawnList;
            for (int i = 0; i < PawnsFinder.AllMapsWorldAndTemporary_Alive.Count; i++)
            {
                Pawn p;
                try
                {
                    p = PawnsFinder.AllMapsWorldAndTemporary_Alive[i];
                }
                catch (ArgumentOutOfRangeException)
                {
                    break;
                }
                if (p.playerSettings != null && p.playerSettings.Master == master)
                {
                    lock (pawnList)
                    {
                        pawnList.Add(p);
                    }
                }
            }
            return pawnList;
        }
	}
}

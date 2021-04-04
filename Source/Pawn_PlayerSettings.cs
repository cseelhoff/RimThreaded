using RimWorld;
using System;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{
    class Pawn_PlayerSettings_Patch
    {
        public static Dictionary<Pawn, List<Pawn>> pets = new Dictionary<Pawn, List<Pawn>>();
        public static bool petsInit = false;
        public static FieldRef<Pawn_PlayerSettings, Pawn> master = FieldRefAccess<Pawn_PlayerSettings, Pawn>("master");
        public static FieldRef<Pawn_PlayerSettings, Pawn> pawn = FieldRefAccess<Pawn_PlayerSettings, Pawn>("pawn");

        public static void RunDestructivePatches()
        {
            Type original = typeof(Pawn_PlayerSettings);
            Type patched = typeof(Pawn_PlayerSettings_Patch);
            RimThreadedHarmony.Prefix(original, patched, "set_Master");
        }

        public static bool set_Master(Pawn_PlayerSettings __instance, Pawn value)
		{
            if (value == null || master(__instance) == value)
            {
                return false;
            }

            if (!pawn(__instance).training.HasLearned(TrainableDefOf.Obedience))
            {
                Log.ErrorOnce("Attempted to set master for non-obedient pawn", 73908573);
                return false;
            }
            if(petsInit == false)
            {
                RebuildPetsDictionary();
            }
            bool flag = ThinkNode_ConditionalShouldFollowMaster.ShouldFollowMaster(pawn(__instance));
            if (master(__instance) != null)
            {
                if (pets.TryGetValue(value, out List<Pawn> pawnList2))
                {
                    pawnList2.Remove(pawn(__instance));
                }
            }
            master(__instance) = value;
            if (!pets.TryGetValue(value, out List<Pawn> pawnList))
            {
                pawnList = new List<Pawn>();
                lock (pets) {
                    pets[value] = pawnList;
                }
            }
            pawnList.Add(pawn(__instance));

            if (pawn(__instance).Spawned && (flag || ThinkNode_ConditionalShouldFollowMaster.ShouldFollowMaster(pawn(__instance))))
            {
                pawn(__instance).jobs.EndCurrentJob(JobCondition.InterruptForced);
            }
            return false;
        }
        public static void RebuildPetsDictionary()
        {
            lock (pets)
            {
                if (petsInit == false)
                {
                    for (int i = 0; i < PawnsFinder.AllMapsWorldAndTemporary_Alive.Count; i++)
                    {
                        Pawn p = PawnsFinder.AllMapsWorldAndTemporary_Alive[i];
                        if (p.playerSettings != null)
                        {
                            Pawn master = p.playerSettings.Master;
                            if (master != null)
                            {
                                if (!pets.TryGetValue(master, out List<Pawn> pawnList))
                                {
                                    pawnList = new List<Pawn>();
                                }
                                pawnList.Add(p);
                            }
                        }
                    }
                    petsInit = true;
                }
            }
        }
	}
}

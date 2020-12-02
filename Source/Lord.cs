using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI.Group;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{
    class Lord_Patch
    {
        public static FieldRef<Lord, LordToil> curLordToil = FieldRefAccess<Lord, LordToil>("curLordToil");
        public static FieldRef<Lord, LordJob> curJob = FieldRefAccess<Lord, LordJob>("curJob");
        public static Dictionary<Pawn, Lord> pawnsLord = new Dictionary<Pawn, Lord>();

        public static bool AddPawn(Lord __instance, Pawn p)
        {
            if (__instance.ownedPawns.Contains(p))
            {
                Log.Error(string.Concat("Lord for ", __instance.faction.ToStringSafe(), " tried to add ", p, " whom it already controls."));
            }
            else if (p.GetLord() != null)
            {
                Log.Error(string.Concat("Tried to add pawn ", p, " to lord ", __instance, " but this pawn is already a member of lord ", p.GetLord(), ". Pawns can't be members of more than one lord at the same time."));
            }
            else
            {
                lock (__instance.ownedPawns)
                {
                    __instance.ownedPawns.Add(p);
                }
                pawnsLord[p] = __instance;
                __instance.numPawnsEverGained++;
                __instance.Map.attackTargetsCache.UpdateTarget(p);
                curLordToil(__instance).UpdateAllDuties();
                curJob(__instance).Notify_PawnAdded(p);
            }
            return false;
        }

        public static bool RemovePawn(Lord __instance, Pawn p)
        {
            lock (__instance.ownedPawns)
            {
                __instance.ownedPawns.Remove(p);
            }
            pawnsLord[p] = null;
            if (p.mindState != null)
            {
                p.mindState.duty = null;
            }

            __instance.Map.attackTargetsCache.UpdateTarget(p);
            return false;
        }


    }
}
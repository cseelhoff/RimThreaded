using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{
    public class MemoryThoughtHandler_Patch
    {

        private static readonly FieldRef<MemoryThoughtHandler, List<Thought_Memory>> memoriesFieldRef = 
            FieldRefAccess<MemoryThoughtHandler, List<Thought_Memory>>("memories");

        public static void RunDestructivePatches()
        {
            Type original = typeof(MemoryThoughtHandler);
            Type patched = typeof(MemoryThoughtHandler_Patch);
            RimThreadedHarmony.Prefix(original, patched, "RemoveMemory");
            //RimThreadedHarmony.Prefix(original, patched, "TryGainMemory", new Type[] { typeof(Thought_Memory), typeof(Pawn) });
        }

        public static bool RemoveMemory(MemoryThoughtHandler __instance, Thought_Memory th)
        {
            lock (__instance)
            {
                List<Thought_Memory> newMemories = new List<Thought_Memory>(__instance.Memories);
                if (!newMemories.Remove(th))
                {
                    Log.Warning("Tried to remove memory thought of def " + th.def.defName + " but it's not here.");
                }
                else
                {
                    memoriesFieldRef(__instance) = newMemories;
                }
            }
            return false;
        }

        public static bool TryGainMemory(MemoryThoughtHandler __instance, Thought_Memory newThought, Pawn otherPawn = null)
        {
            if (!ThoughtUtility.CanGetThought_NewTemp(__instance.pawn, newThought.def))
            {
                return false;
            }

            if (newThought is Thought_MemorySocial && newThought.otherPawn == null && otherPawn == null)
            {
                Log.Error(string.Concat("Can't gain social thought ", newThought.def, " because its otherPawn is null and otherPawn passed to this method is also null. Social thoughts must have otherPawn."));
                return false;
            }

            newThought.pawn = __instance.pawn;
            newThought.otherPawn = otherPawn;
            if (!newThought.TryMergeWithExistingMemory(out bool showBubble))
            {
                lock (__instance) //ADDED
                {
                    memoriesFieldRef(__instance).Add(newThought);
                }
            }

            if (newThought.def.stackLimitForSameOtherPawn >= 0)
            {
                while (__instance.NumMemoriesInGroup(newThought) > newThought.def.stackLimitForSameOtherPawn)
                {
                    __instance.RemoveMemory(__instance.OldestMemoryInGroup(newThought));
                }
            }

            if (newThought.def.stackLimit >= 0)
            {
                while (__instance.NumMemoriesOfDef(newThought.def) > newThought.def.stackLimit)
                {
                    __instance.RemoveMemory(__instance.OldestMemoryOfDef(newThought.def));
                }
            }

            if (newThought.def.thoughtToMake != null)
            {
                __instance.TryGainMemory(newThought.def.thoughtToMake, newThought.otherPawn);
            }

            if (showBubble && newThought.def.showBubble && __instance.pawn.Spawned && PawnUtility.ShouldSendNotificationAbout(__instance.pawn))
            {
                MoteMaker.MakeMoodThoughtBubble(__instance.pawn, newThought);
            }
            return false;
        }

    }
}

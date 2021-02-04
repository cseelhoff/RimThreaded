using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{
    public class MemoryThoughtHandler_Patch
    {
        static Action<MemoryThoughtHandler> actionRemoveExpiredMemories = (Action<MemoryThoughtHandler>)Delegate.CreateDelegate
            (typeof(Action<MemoryThoughtHandler>), Method(typeof(MemoryThoughtHandler), "RemoveExpiredMemories"));

        public static bool MemoryThoughtInterval(MemoryThoughtHandler __instance)
        {
            for (int i = 0; i < __instance.Memories.Count; i++)
            {
                Thought_Memory memory;
                try
                {
                    memory = __instance.Memories[i];
                }
                catch (ArgumentOutOfRangeException)
                {
                    break;
                }
                if(memory != null)
                    memory.ThoughtInterval();
            }

            actionRemoveExpiredMemories(__instance);
            return false;
        }

        public static bool ExposeData(MemoryThoughtHandler __instance)
        {
            List<Thought_Memory> memories = __instance.Memories;
            Scribe_Collections.Look(ref memories, "memories", LookMode.Deep);
            if (Scribe.mode != LoadSaveMode.PostLoadInit)
            {
                return false;
            }

            for (int num = memories.Count - 1; num >= 0; num--)
            {
                Thought_Memory memory = memories[num];
                if (memory == null || memory.def == null)
                {
                    memories.RemoveAt(num);
                }
                else
                {
                    memory.pawn = __instance.pawn;
                }
            }
            return false;
        }

    }
}

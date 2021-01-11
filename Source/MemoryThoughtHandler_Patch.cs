using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    }
}

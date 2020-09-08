using HarmonyLib;
using System.Linq;
using Verse;
using System.Collections.Concurrent;
using System.Reflection;

namespace RimThreaded
{

    public static class Rand_Patch
    {
        private static ConcurrentStack<ulong> stateStack = new ConcurrentStack<ulong>();
        public static PropertyInfo stateCompressed = AccessTools.DeclaredProperty(typeof(Rand), "StateCompressed");
        public static uint seed = AccessTools.StaticFieldRefAccess<uint>(typeof(Rand), "seed");
        public static uint iterations = AccessTools.StaticFieldRefAccess<uint>(typeof(Rand), "iterations");

        public static bool set_Seed(uint value)
        {
            if (stateStack.Count == 0)
                Log.ErrorOnce("Modifying the initial rand seed. Call PushState() first. The initial rand seed should always be based on the startup time and set only once.", 825343540, false);
            seed = value;
            iterations = 0U;
            return false;
        }


        public static bool EnsureStateStackEmpty()
        {
            if (stateStack.Count <= 0)
                return false;
            Log.Warning("Random state stack is not empty. There were more calls to PushState than PopState. Fixing.", false);
            while (stateStack.Any())
                PopState();
            return false;
        }

        public static bool PushState()
        {            
            stateStack.Push((ulong)stateCompressed.GetValue(null, null));
            return false;
        }

        public static bool PopState()
        {
            if (stateStack.TryPop(out ulong result))
            {
                stateCompressed.SetValue(null, result, null);
            }
            else
            {
                Log.Error("Rand.PopState stateStack.TryPop failed");
            }
            return false;
        }

    }

}

using RimWorld;
using System;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using static HarmonyLib.AccessTools;

namespace RimThreaded
{
    class TaleManager_Patch
    {
        public static FieldRef<TaleManager, List<Tale>> tales = FieldRefAccess<TaleManager, List<Tale>>("tales");

        private static readonly MethodInfo methodCheckCullTales =
            Method(typeof(TaleManager), "CheckCullTales", new Type[] { typeof(Tale) });
        private static readonly Action<TaleManager, Tale> actionCheckCullTales =
            (Action<TaleManager, Tale>)Delegate.CreateDelegate(
                typeof(Action<TaleManager, Tale>), methodCheckCullTales);

        public static void RunDestructivePatches()
        {
            Type original = typeof(TaleManager);
            Type patched = typeof(TaleManager_Patch);
            RimThreadedHarmony.Prefix(original, patched, "Add");
            RimThreadedHarmony.Prefix(original, patched, "RemoveTale");
        }

        public static bool Add(TaleManager __instance, Tale tale)
        {
            lock (__instance)
            {
                tales(__instance).Add(tale);
            }
            actionCheckCullTales(__instance, tale);
            return false;
        }
        public static bool RemoveTale(TaleManager __instance, Tale tale)
        {
            if (!tale.Unused)
            {
                Log.Warning("Tried to remove used tale " + tale);
            }
            else
            {
                lock (__instance)
                {
                    List<Tale> newTales = new List<Tale>(tales(__instance));
                    newTales.Remove(tale);
                    tales(__instance) = newTales;
                }
            }
            return false;
        }


    }
}

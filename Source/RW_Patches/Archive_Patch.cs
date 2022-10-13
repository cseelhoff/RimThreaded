using RimWorld;
using System;
using Verse;

namespace RimThreaded.RW_Patches
{
    class Archive_Patch
    {
        internal static void RunDestructivePatches()
        {
            Type original = typeof(Archive);
            Type patched = typeof(Archive_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(ExposeData));
            RimThreadedHarmony.Prefix(original, patched, nameof(Add));
            RimThreadedHarmony.Prefix(original, patched, nameof(Remove));
            RimThreadedHarmony.Prefix(original, patched, nameof(Contains));
        }

        public static bool ExposeData(Archive __instance)
        {
            lock (__instance) //changed
            {
                Scribe_Collections.Look(ref __instance.archivables, "archivables", LookMode.Deep, Array.Empty<object>());
                Scribe_Collections.Look(ref __instance.pinnedArchivables, "pinnedArchivables", LookMode.Reference);
                if (Scribe.mode != LoadSaveMode.PostLoadInit)
                    return false;
                __instance.archivables.RemoveAll(x => x == null);
                __instance.pinnedArchivables.RemoveWhere(x => x == null);
                return false;
            }
        }
        public static bool Add(Archive __instance, ref bool __result, IArchivable archivable)
        {
            lock (__instance) //changed
            {
                if (archivable == null)
                {
                    Log.Error("Tried to add null archivable.");
                    __result = false;
                    return false;
                }
                if (__instance.Contains(archivable))
                {
                    __result = false;
                    return false;
                }
                __instance.archivables.Add(archivable);
                __instance.archivables.SortBy(x => x.CreatedTicksGame);
                __instance.CheckCullArchivables();
                __result = true;
                return false;
            }
        }

        public static bool Remove(Archive __instance, ref bool __result, IArchivable archivable)
        {
            lock (__instance) //changed
            {
                if (!__instance.Contains(archivable))
                {
                    __result = false;
                    return false;
                }
                __instance.archivables.Remove(archivable);
                __instance.pinnedArchivables.Remove(archivable);
                __result = true;
                return false;
            }
        }

        public static bool Contains(Archive __instance, ref bool __result, IArchivable archivable)
        {
            lock (__instance) //changed
            {
                __result = __instance.archivables.Contains(archivable);
                return false;
            }
        }

    }
}

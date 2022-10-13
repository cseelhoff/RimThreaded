using RimThreaded.RW_Patches;
using RimThreaded;
using System;
using Verse;
using System.Collections.Generic;

public class BodyDef_Patch
{
    internal static void RunDestructivePatches()
    {
        Type original = typeof(BodyDef);
        Type patched = typeof(BodyDef_Patch);
        RimThreadedHarmony.Prefix(original, patched, nameof(GetPartsWithTag));
    }

    public static bool GetPartsWithTag(BodyDef __instance, ref List<BodyPartRecord> __result, BodyPartTagDef tag)
    {
        Dictionary<BodyPartTagDef, List<BodyPartRecord>> cachedPartsByTag = __instance.cachedPartsByTag;

        if (cachedPartsByTag.TryGetValue(tag, out __result))
            return false;

        lock (cachedPartsByTag)
        {
            if (cachedPartsByTag.TryGetValue(tag, out __result))
                return false;
            List<BodyPartRecord> AllParts = __instance.AllParts;
            __result = new List<BodyPartRecord>();
            for (int i = 0; i < AllParts.Count; i++)
            {
                BodyPartRecord bodyPartRecord = AllParts[i];
                if (bodyPartRecord.def.tags.Contains(tag))
                {
                    __result.Add(bodyPartRecord);
                }
            }
            cachedPartsByTag[tag] = __result;
        }
        return false;
    }

}

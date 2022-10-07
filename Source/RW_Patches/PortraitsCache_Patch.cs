using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;
using static RimWorld.PortraitsCache;

namespace RimThreaded.RW_Patches
{
    public class PortraitsCache_Patch
    {
        internal static void RunDestructivePatches()
        {
            Type original = typeof(PortraitsCache);
            Type patched = typeof(PortraitsCache_Patch);
            /*
            RimThreadedHarmony.Prefix(original, patched, nameof(Clear));
            RimThreadedHarmony.Prefix(original, patched, nameof(GetOrCreateCachedPortraitsWithParams));
            RimThreadedHarmony.Prefix(original, patched, nameof(NewRenderTexture));
            RimThreadedHarmony.Prefix(original, patched, nameof(RemoveExpiredCachedPortraits));
            RimThreadedHarmony.Prefix(original, patched, nameof(Get));
            RimThreadedHarmony.Prefix(original, patched, nameof(SetAnimatedPortraitsDirty));
            RimThreadedHarmony.Prefix(original, patched, nameof(SetDirty));
            */
        }
        
    }
}

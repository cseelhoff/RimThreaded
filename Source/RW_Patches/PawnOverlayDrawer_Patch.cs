using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using static RimWorld.PawnOverlayDrawer;

namespace RimThreaded.RW_Patches
{
    internal class PawnOverlayDrawer_Patch
    {
        internal static void RunDestructivePatches()
        {
            Type original = typeof(PawnOverlayDrawer);
            Type patched = typeof(PawnOverlayDrawer_Patch);
            RimThreadedHarmony.Prefix(original, patched, nameof(ClearCache));
            RimThreadedHarmony.Prefix(original, patched, nameof(RenderPawnOverlay));
        }
        public static bool ClearCache(PawnOverlayDrawer __instance)
        {
            //added lock
            //Dictionary<CacheKey, List<DrawCall>> drawCallCache = __instance.drawCallCache;
            lock (__instance.drawCallCache)
            {
                foreach (List<DrawCall> value in __instance.drawCallCache.Values)
                {
                    ReturnDrawCallList(value);
                }
                __instance.drawCallCache.Clear();
            }
            return false;
        }

        public static bool RenderPawnOverlay(PawnOverlayDrawer __instance, Vector3 drawLoc, Mesh bodyMesh, Quaternion quat, bool drawNow, OverlayLayer layer, Rot4 pawnRot, bool? overApparel = null)
        {
            CacheKey key = new CacheKey(drawLoc, bodyMesh, quat, pawnRot, layer);
            //added lock
            lock (__instance.drawCallCache)
            {
                if (!__instance.drawCallCache.TryGetValue(key, out var value))
                {
                    value = GetDrawCallList();
                    __instance.WriteCache(key, value);
                    __instance.drawCallCache.Add(key, value);
                } else if(value == null)
                {
                    __instance.drawCallCache.Remove(key);
                    return false;
                }
                foreach (DrawCall item in value)
                {
                    if (!overApparel.HasValue || overApparel == item.displayOverApparel)
                    {
                        __instance.DoDrawCall(item, drawNow);
                    }
                }
            }
            return false;
        }
    }
}

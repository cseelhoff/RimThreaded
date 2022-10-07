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
            RimThreadedHarmony.Prefix(original, patched, nameof(Clear));
            RimThreadedHarmony.Prefix(original, patched, nameof(GetOrCreateCachedPortraitsWithParams));
            RimThreadedHarmony.Prefix(original, patched, nameof(NewRenderTexture));
            RimThreadedHarmony.Prefix(original, patched, nameof(RemoveExpiredCachedPortraits));
            RimThreadedHarmony.Prefix(original, patched, nameof(Get));
            RimThreadedHarmony.Prefix(original, patched, nameof(SetAnimatedPortraitsDirty));
            RimThreadedHarmony.Prefix(original, patched, nameof(SetDirty));
        }
        public static bool Get(ref RenderTexture __result, Pawn pawn, Vector2 size, Rot4 rotation, Vector3 cameraOffset = default, float cameraZoom = 1f, bool supersample = true, bool compensateForUIScale = true, bool renderHeadgear = true, bool renderClothes = true, Dictionary<Apparel, Color> overrideApparelColors = null, Color? overrideHairColor = null, bool stylingStation = false)
        {
            if (supersample)
            {
                size *= 1.25f;
            }
            if (compensateForUIScale)
            {
                size *= Prefs.UIScale;
            }
            Dictionary<Pawn, CachedPortrait> dictionary = PortraitsCache.GetOrCreateCachedPortraitsWithParams(size, cameraOffset, cameraZoom, rotation, renderHeadgear, renderClothes, overrideApparelColors, overrideHairColor, stylingStation).CachedPortraits;
            if (dictionary.TryGetValue(pawn, out var value))
            {
                if (!value.RenderTexture.IsCreated())
                {
                    value.RenderTexture.Create();
                    RenderPortrait(pawn, value.RenderTexture, cameraOffset, cameraZoom, rotation, renderHeadgear, renderClothes, overrideApparelColors, overrideHairColor, stylingStation);
                }
                else if (value.Dirty)
                {
                    RenderPortrait(pawn, value.RenderTexture, cameraOffset, cameraZoom, rotation, renderHeadgear, renderClothes, overrideApparelColors, overrideHairColor, stylingStation);
                }
                dictionary.Remove(pawn);
                dictionary.Add(pawn, new CachedPortrait(value.RenderTexture, dirty: false, Time.time));
                __result = value.RenderTexture;
                return false;
            }
            RenderTexture renderTexture = PortraitsCache.NewRenderTexture(size);
            RenderPortrait(pawn, renderTexture, cameraOffset, cameraZoom, rotation, renderHeadgear, renderClothes, overrideApparelColors, overrideHairColor, stylingStation);
            dictionary.Add(pawn, new CachedPortrait(renderTexture, dirty: false, Time.time));
            __result = value.RenderTexture;
            return false;
        }
        public static bool SetDirty(Pawn pawn)
        {
            for (int i = 0; i < cachedPortraits.Count; i++)
            {
                Dictionary<Pawn, CachedPortrait> dictionary = cachedPortraits[i].CachedPortraits;
                lock (dictionary)
                {
                    if (dictionary.TryGetValue(pawn, out var value) && !value.Dirty)
                    {
                        dictionary.Remove(pawn);
                        dictionary.Add(pawn, new CachedPortrait(value.RenderTexture, dirty: true, value.LastUseTime));
                    }
                }
            }
            return false;
        }
        public static bool RemoveExpiredCachedPortraits()
        {
            lock (renderTexturesPool)
            {
                for (int i = 0; i < cachedPortraits.Count; i++)
                {
                    Dictionary<Pawn, CachedPortrait> dictionary = cachedPortraits[i].CachedPortraits;
                    toRemove.Clear();
                    lock (dictionary)
                    {
                        foreach (KeyValuePair<Pawn, CachedPortrait> item in dictionary)
                        {
                            if (item.Value.Expired)
                            {
                                toRemove.Add(item.Key);
                                renderTexturesPool.Add(item.Value.RenderTexture);
                            }
                        }
                        for (int j = 0; j < toRemove.Count; j++)
                        {
                            dictionary.Remove(toRemove[j]);
                        }
                    }
                    toRemove.Clear();
                }
            }
            return false;
        }
        public static bool NewRenderTexture(ref RenderTexture __result, Vector2 size)
        {
            lock (renderTexturesPool)
            {
                int num = renderTexturesPool.FindLastIndex((x) => x.width == (int)size.x && x.height == (int)size.y);
                if (num != -1)
                {
                    RenderTexture result = renderTexturesPool[num];
                    renderTexturesPool.RemoveAt(num);
                    __result = result;
                    return false;
                }
                __result = new RenderTexture((int)size.x, (int)size.y, 24)//this one doesn't have a parameterless constructor... I could do a specific pool for him but I am not going to, this is already a Pool for textures.
                {
                    name = "Portrait",
                    useMipMap = false,
                    filterMode = FilterMode.Bilinear
                };
            }
            return false;
        }

        public static bool Clear()
        {
            for (int i = 0; i < cachedPortraits.Count; i++)
            {
                foreach (KeyValuePair<Pawn, CachedPortrait> cachedPortrait in cachedPortraits[i].CachedPortraits)
                {
                    DestroyRenderTexture(cachedPortrait.Value.RenderTexture);
                }
                //SimplePool_Patch<Dictionary<Pawn, CachedPortrait>>.Return(cachedPortraits[i].CachedPortraits);
                //SimplePool_Patch<CachedPortraitsWithParams>.Return(cachedPortraits[i]);
            }
            //in this case the one tick pool can't be used because the execution of this method can come directly from outside the abstraction of the game "tick".
            //the PulsePool should provide enough elements to avoid race conditions (or make them extreamly rare) while avoiding garbage.
            cachedPortraits = PulsePool<List<CachedPortraitsWithParams>>.Pulse(cachedPortraits);//OneTickPool<List<CachedPortraitsWithParams>>.Get();
            cachedPortraits.Clear();
            for (int j = 0; j < renderTexturesPool.Count; j++)
            {
                DestroyRenderTexture(renderTexturesPool[j]);
            }
            renderTexturesPool = PulsePool<List<RenderTexture>>.Pulse(renderTexturesPool);//OneTickPool<List<RenderTexture>>.Get();
            renderTexturesPool.Clear();
            return false;
        }
        /*
		public static bool GetOrCreateCachedPortraitsWithParams(ref CachedPortraitsWithParams __result, Vector2 size, Vector3 cameraOffset, float cameraZoom, Rot4 rotation, bool renderHeadgear = true, bool renderClothes = true, Dictionary<Apparel, Color> overrideApparelColors = null, Color? overrideHairColor = null, bool stylingStation = false)
        {
			List<CachedPortraitsWithParams> tmpcache_Initial = SimplePool_Patch<List<CachedPortraitsWithParams>>.Get();
			List<CachedPortraitsWithParams> tmpcache_AfterAdd = SimplePool_Patch<List<CachedPortraitsWithParams>>.Get();
			tmpcache_Initial.Clear();
			tmpcache_AfterAdd.Clear();
			do
			{
				tmpcache_Initial.AddRange(cachedPortraits);
				for (int i = 0; i < tmpcache_Initial.Count; i++)
				{
					if (tmpcache_Initial[i].Size == size && tmpcache_Initial[i].CameraOffset == cameraOffset && tmpcache_Initial[i].CameraZoom == cameraZoom && tmpcache_Initial[i].Rotation == rotation && tmpcache_Initial[i].RenderHeadgear == renderHeadgear && cachedPortraits[i].RenderClothes == renderClothes && tmpcache_Initial[i].StylingStation == stylingStation && tmpcache_Initial[i].OverrideHairColor == overrideHairColor && GenCollection.DictsEqual(tmpcache_Initial[i].OverrideApparelColors, overrideApparelColors))
					{
						__result = tmpcache_Initial[i];
						SimplePool_Patch<List<CachedPortraitsWithParams>>.Return(tmpcache_Initial);
						SimplePool_Patch<List<CachedPortraitsWithParams>>.Return(tmpcache_AfterAdd);
						return false;
					}
				}
				//simplepool_patch here
				tmpcache_AfterAdd.AddRange(tmpcache_Initial);
				CachedPortraitsWithParams cachedPortraitsWithParams = new CachedPortraitsWithParams(size, cameraOffset, cameraZoom, rotation, renderHeadgear, renderClothes, overrideApparelColors, overrideHairColor, stylingStation);
				tmpcache_AfterAdd.Add(cachedPortraitsWithParams);
				//cachedPortraits.Add(cachedPortraitsWithParams);
				__result = cachedPortraitsWithParams;
			}
			while (tmpcache_Initial != Interlocked.CompareExchange<List<CachedPortraitsWithParams>>(ref cachedPortraits, tmpcache_AfterAdd, tmpcache_Initial));
			SimplePool_Patch<List<CachedPortraitsWithParams>>.Return(tmpcache_Initial);
			SimplePool_Patch<List<CachedPortraitsWithParams>>.Return(tmpcache_AfterAdd);
			return false;
		}*/

        public static bool SetAnimatedPortraitsDirty()
        {
            for (int i = 0; i < cachedPortraits.Count; i++)
            {
                Dictionary<Pawn, CachedPortrait> dictionary = cachedPortraits[i].CachedPortraits;
                toSetDirty.Clear();
                lock (dictionary)
                {
                    foreach (KeyValuePair<Pawn, CachedPortrait> item in dictionary)
                    {
                        if (IsAnimated(item.Key) && !item.Value.Dirty)
                        {
                            toSetDirty.Add(item.Key);
                        }
                    }
                }
                if (toSetDirty.Count > 0)
                {
                    lock (dictionary)
                    {
                        for (int j = 0; j < toSetDirty.Count; j++)
                        {
                            CachedPortrait cachedPortrait = dictionary[toSetDirty[j]];
                            dictionary.Remove(toSetDirty[j]);
                            dictionary.Add(toSetDirty[j], new CachedPortrait(cachedPortrait.RenderTexture, dirty: true, cachedPortrait.LastUseTime));
                        }
                    }
                }
                toSetDirty.Clear();
            }
            return false;
        }
        public static bool GetOrCreateCachedPortraitsWithParams(ref CachedPortraitsWithParams __result, Vector2 size, Vector3 cameraOffset, float cameraZoom, Rot4 rotation, bool renderHeadgear = true, bool renderClothes = true, Dictionary<Apparel, Color> overrideApparelColors = null, Color? overrideHairColor = null, bool stylingStation = false)
        {
            for (int i = 0; i < cachedPortraits.Count; i++)
            {
                if (cachedPortraits[i].Size == size && cachedPortraits[i].CameraOffset == cameraOffset && cachedPortraits[i].CameraZoom == cameraZoom && cachedPortraits[i].Rotation == rotation && cachedPortraits[i].RenderHeadgear == renderHeadgear && cachedPortraits[i].RenderClothes == renderClothes && cachedPortraits[i].StylingStation == stylingStation && cachedPortraits[i].OverrideHairColor == overrideHairColor && GenCollection.DictsEqual(cachedPortraits[i].OverrideApparelColors, overrideApparelColors))
                {
                    __result = cachedPortraits[i];
                    return false;
                }
            }
            lock (cachedPortraits)
            {
                for (int i = 0; i < cachedPortraits.Count; i++)
                {
                    if (cachedPortraits[i].Size == size && cachedPortraits[i].CameraOffset == cameraOffset && cachedPortraits[i].CameraZoom == cameraZoom && cachedPortraits[i].Rotation == rotation && cachedPortraits[i].RenderHeadgear == renderHeadgear && cachedPortraits[i].RenderClothes == renderClothes && cachedPortraits[i].StylingStation == stylingStation && cachedPortraits[i].OverrideHairColor == overrideHairColor && GenCollection.DictsEqual(cachedPortraits[i].OverrideApparelColors, overrideApparelColors))
                    {
                        __result = cachedPortraits[i];
                        return false;
                    }
                }
                CachedPortraitsWithParams cachedPortraitsWithParams = new CachedPortraitsWithParams(size, cameraOffset, cameraZoom, rotation, renderHeadgear, renderClothes, overrideApparelColors, overrideHairColor, stylingStation);
                /* Belive me you don't want to fuck around with this for an incredible amount of reasons.
				CachedPortraitsWithParams cachedPortraitsWithParams = SimplePool_Patch<CachedPortraitsWithParams>.Get();
				cachedPortraitsWithParams.CachedPortraits = SimplePool_Patch<Dictionary<Pawn, CachedPortrait>>.Get();
				cachedPortraitsWithParams.Size = size;
				cachedPortraitsWithParams.CameraOffset = cameraOffset;
				cachedPortraitsWithParams.CameraZoom = cameraZoom;
				cachedPortraitsWithParams.Rotation = rotation;
				cachedPortraitsWithParams.RenderHeadgear = renderHeadgear;
				cachedPortraitsWithParams.RenderClothes = renderClothes;
				cachedPortraitsWithParams.OverrideApparelColors = overrideApparelColors;
				cachedPortraitsWithParams.OverrideHairColor = overrideHairColor;
				cachedPortraitsWithParams.StylingStation = stylingStation;*/

                cachedPortraits.Add(cachedPortraitsWithParams);

                __result = cachedPortraitsWithParams;
            }
            return false;
        }
    }
}

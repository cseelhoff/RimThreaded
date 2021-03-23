using HarmonyLib;
using System.Collections.Generic;
using Verse;
using System.Reflection;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;

namespace RimThreaded
{
    public class ThingOwnerThing_Patch
    {
        public static AccessTools.FieldRef<ThingOwner<Thing>, List<Thing>> innerList =
            AccessTools.FieldRefAccess<ThingOwner<Thing>, List<Thing>>("innerList");
        public static AccessTools.FieldRef<ThingOwner, int> maxStacks =
            AccessTools.FieldRefAccess<ThingOwner, int>("maxStacks");
        public static MethodInfo NotifyRemoved =
            typeof(ThingOwner<Thing>).GetMethod("NotifyRemoved", BindingFlags.NonPublic | BindingFlags.Instance);

        public static bool Remove(ThingOwner<Thing> __instance, ref bool __result, Thing item)
        {
            if (!__instance.Contains(item))
            {
                __result = false;
                return false;
            }
            if (item.holdingOwner == __instance)
                item.holdingOwner = null;
            lock (innerList(__instance))
            {
                int removeIndex = innerList(__instance).LastIndexOf(item);
                if (removeIndex == -1)
                {
                    __result = false;
                    return false;
                }
                innerList(__instance).RemoveAt(removeIndex);
            }
            //NotifyRemoved2(__instance, item);
            NotifyRemoved.Invoke(__instance, new object[] { item });            
            __result = true;
            return false;
        }

        public static bool TryAdd(ThingOwner<Thing> __instance, ref bool __result, Thing item, bool canMergeWithExistingStacks = true)
        {
            if (item == null)
            {
                Log.Warning("Tried to add null item to ThingOwner.", false);
                __result = false;
                return false;
            }
            if (!(item is Thing obj))
            {
                __result = false;
                return false;
            }
            if (__instance.Contains(item))
            {
                Log.Warning("Tried to add " + item.ToStringSafe() + " to ThingOwner but this item is already here.", false);
                __result = false;
                return false;
            }
            if (item.holdingOwner != null)
            {
                Log.Warning("Tried to add " + item.ToStringSafe() + " to ThingOwner but this thing is already in another container. owner=" + __instance.Owner.ToStringSafe() + ", current container owner=" + item.holdingOwner.Owner.ToStringSafe() + ". Use TryAddOrTransfer, TryTransferToContainer, or remove the item before adding it.", false);
                __result = false;
                return false;
            }
            if (!__instance.CanAcceptAnyOf(item, canMergeWithExistingStacks))
            {
                __result = false;
                return false;
            }
            if (canMergeWithExistingStacks)
            {
                for (int index = 0; index < innerList(__instance).Count; ++index)
                {
                    Thing inner = innerList(__instance)[index];
                    if (inner.CanStackWith(item))
                    {
                        int count = Mathf.Min(item.stackCount, inner.def.stackLimit - inner.stackCount);
                        if (count > 0)
                        {
                            Thing other = item.SplitOff(count);
                            int stackCount = inner.stackCount;
                            inner.TryAbsorbStack(other, true);
                            if (inner.stackCount > stackCount)
                                NotifyAddedAndMergedWith(__instance, inner, inner.stackCount - stackCount);
                            if (item.Destroyed || item.stackCount == 0)
                            {
                                __result = true;
                                return false;
                            }
                        }
                    }
                }
            }
            if (__instance.Count >= maxStacks(__instance))
            {
                __result = false;
                return false;
            }
            item.holdingOwner = __instance;
            lock (innerList(__instance))
            {
                innerList(__instance).Add(obj);
            }
            NotifyAdded(__instance, obj);
            __result = true;
            return false;

        }
        private static void NotifyAddedAndMergedWith(ThingOwner<Thing> __instance, Thing item, int mergedCount)
        {
            if (!(__instance.Owner is CompTransporter owner))
                return;
            owner.Notify_ThingAddedAndMergedWith(item, mergedCount);
        }

        private static void NotifyAdded(ThingOwner<Thing> __instance, Thing item)
        {
            if (ThingOwnerUtility.ShouldAutoExtinguishInnerThings(__instance.Owner) && item.HasAttachment(ThingDefOf.Fire))
                item.GetAttachment(ThingDefOf.Fire).Destroy(DestroyMode.Vanish);
            if (ThingOwnerUtility.ShouldRemoveDesignationsOnAddedThings(__instance.Owner))
            {
                List<Map> maps = Find.Maps;
                for (int index = 0; index < maps.Count; ++index)
                    maps[index].designationManager.RemoveAllDesignationsOn(item, false);
            }
            if (__instance.Owner is CompTransporter owner2)
                owner2.Notify_ThingAdded(item);
            if (__instance.Owner is Caravan owner3)
                owner3.Notify_PawnAdded((Pawn)item);
            if (__instance.Owner is Pawn_ApparelTracker owner4)
                owner4.Notify_ApparelAdded((Apparel)item);
            if (__instance.Owner is Pawn_EquipmentTracker owner5)
                owner5.Notify_EquipmentAdded((ThingWithComps)item);
            NotifyColonistBarIfColonistCorpse(item);
        }
        private static void NotifyColonistBarIfColonistCorpse(Thing thing)
        {
            if (!(thing is Corpse corpse) || corpse.Bugged || (corpse.InnerPawn.Faction == null || !corpse.InnerPawn.Faction.IsPlayer) || Current.ProgramState != ProgramState.Playing)
                return;
            Find.ColonistBar.MarkColonistsDirty();
        }

        /*
        private static void NotifyRemoved2(ThingOwner<Thing> __instance, Thing item)
        {
            IThingHolder this_owner = owner(__instance);
            if (this_owner is Pawn_InventoryTracker owner2)
                owner2.Notify_ItemRemoved(item);
            if (this_owner is Pawn_ApparelTracker owner3)
                owner3.Notify_ApparelRemoved((Apparel)item);
            if (this_owner is Pawn_EquipmentTracker owner4)
                owner4.Notify_EquipmentRemoved((ThingWithComps)item);
            if (this_owner is Caravan owner5)
                owner5.Notify_PawnRemoved((Pawn)item);
            NotifyColonistBarIfColonistCorpse2(item);
        }
        private static void NotifyColonistBarIfColonistCorpse2(Thing thing)
        {
            if (!(thing is Corpse corpse) || corpse.Bugged || (corpse.InnerPawn.Faction == null || !corpse.InnerPawn.Faction.IsPlayer) || Current.ProgramState != ProgramState.Playing)
                return;
            Find.ColonistBar.MarkColonistsDirty();
        }
        */
    }
}
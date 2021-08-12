using RimWorld;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using Verse;

namespace RimThreaded
{
    public struct FakeStack
    {
        public int s;
        public bool IsEmpty;
    }
    //I will also have to check for hoff patches on the methods we remake here.
    class EntityCache//doubts about catching the difference between Thing and ThingsWithComps... minified Thing may not be important enough to deserve a spot in the cache.
    {
        public static int MAXIMUMCACHABLEENTITIES = 16384;
        public static Dictionary<int, Thing> IndexToEntity = new Dictionary<int, Thing>();
        public static Dictionary<Thing, int> EntityToIndex = new Dictionary<Thing, int>();
        public static Dictionary<int , bool> IsFreeIndex = new Dictionary<int, bool>();//future optimization ((i & (1 << (n - 1))) > 0) to check n-th bit being 1 or 0
        public static Map[] Map;
        public static bool[] IsForbiddenFactionOfPlayer; // this will require a postfix to ForbidUtility.SetForbidden most likely. IMPORTANT
        public static bool[] Spawned;
        public static int[] stackCount;
        public static IntVec3[] Position;
        public static IThingHolder[] ParentHolder;
        public static ThingDef[] def;
        public static int[] HitPoints;
        public static int[] MaxHitPoints;
        public static sbyte[] mapIndexOrState;// this will require a postfix to Thing.SpawnSetup and Thing.Despawn && ThingWithComp.SpawnSetup and ThingWithComp.DeSpawn
        public static ThingDef[] StuffInt;
        public static Dictionary<ThingDef, int> stackLimit = new Dictionary<ThingDef, int>();
        public static Dictionary<ThingDef, bool> defEverHaulable = new Dictionary<ThingDef, bool>();
        public static Dictionary<ThingDef, bool> defalwaysHaulable = new Dictionary<ThingDef, bool>();
        public static void InitializeEntityCacheBuffer()
        {
            Map = new Map[MAXIMUMCACHABLEENTITIES];//64 kB assuming these are 16384 4B pointers
            IsForbiddenFactionOfPlayer = new bool[MAXIMUMCACHABLEENTITIES];
            stackCount = new int[MAXIMUMCACHABLEENTITIES];
            Spawned = new bool[MAXIMUMCACHABLEENTITIES];
            Position = new IntVec3[MAXIMUMCACHABLEENTITIES];
            ParentHolder = new IThingHolder[MAXIMUMCACHABLEENTITIES];
            def = new ThingDef[MAXIMUMCACHABLEENTITIES];
            HitPoints = new int[MAXIMUMCACHABLEENTITIES];
            MaxHitPoints = new int[MAXIMUMCACHABLEENTITIES];
            mapIndexOrState = new sbyte[MAXIMUMCACHABLEENTITIES];
            StuffInt = new ThingDef[MAXIMUMCACHABLEENTITIES];
            for (int i = 0; i != MAXIMUMCACHABLEENTITIES; i++)
            {
                IsFreeIndex[i] = true;//remember to set to false when adding and setting to true when removing.
            }
        }

        public void Add(Thing t)//add and remove must possibly need to be postfixed in Thing.Spawn and Thing.Despawn.
        {
            if (IndexToEntity.Keys.Count == MAXIMUMCACHABLEENTITIES)
            {
                Log.Error("EntityCache tryed to add while already full.");
                return;
            }
            int i = FirstAvailableIndex();
            if (i != -1 && i < MAXIMUMCACHABLEENTITIES)
            {
                IsFreeIndex[i] = false;
                IndexToEntity[i] = t;
                EntityToIndex[t] = i;
                Map[i] = t.Map;
                IsForbiddenFactionOfPlayer[i] = t.IsForbidden(Faction.OfPlayer);
                stackCount[i] = t.stackCount;
                Spawned[i] = t.Spawned;
                Position[i] = t.Position;
                ParentHolder[i] = t.ParentHolder;
                def[i] = t.def;
                HitPoints[i] = t.HitPoints;
                MaxHitPoints[i] = t.MaxHitPoints;
                mapIndexOrState[i] = t.mapIndexOrState;//this requires an add in Thing.SpawnSetup
                StuffInt[i] = t.stuffInt;
                stackLimit[def[i]] = t.def.stackLimit;
                defEverHaulable[def[i]] = t.def.EverHaulable;
                defalwaysHaulable[def[i]] = t.def.alwaysHaulable;
                return;
            }
            Log.Error("EntityCache.FirstAvailableIndex returned an inconsistent index while adding.");
            return;
        }
        public void Remove(Thing t)
        {
#if DEBUG
            if (AlreadyRemoved(t))
            {
                Log.Message("EntityCache.Remove tryed to remove an already removed entity.");
                return;
            }
#endif
            IsFreeIndex[EntityToIndex[t]] = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool AlreadyRemoved(Thing t)
        {
            return IsFreeIndex[EntityToIndex[t]];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int FirstAvailableIndex()
        {
            for (int i = 0; i != MAXIMUMCACHABLEENTITIES; i++)
            {
                if (IsFreeIndex[i])
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// This method implements StoreUtility.CurrentStoragePriorityOf but works internally to the EntityCache
        /// </summary>
        public StoragePriority CurrentStoragePriorityOfIndex(int t) => StoragePriorityAtForIndex(CurrentHaulDestinationOfIndex(t), t);

        /// <summary>
        /// This method implements StoreUtility.CurrentHaulDestinationOfIndex but works internally to the EntityCache
        /// </summary>
        public IHaulDestination CurrentHaulDestinationOfIndex(int t)
        {
            if (!IndexToEntity.ContainsKey(t))
                return null;
            if (!Spawned[t])
            {
                return ParentHolder[t] as IHaulDestination;
            }
            Map map = Map[t];
            if (map == null)
                return null;
            HaulDestinationManager haulDestinationManager = map.haulDestinationManager;
            if (haulDestinationManager == null)
                return null;
            return haulDestinationManager.SlotGroupParentAt(Position[t]);
        }
        /// <summary>
        /// This method implements StoreUtility.StoragePriorityAtFor but works internally to the EntityCache
        /// </summary>
        public StoragePriority StoragePriorityAtForIndex(IHaulDestination at, int t)
        {
            if (at.GetType() == typeof(Building_Casket))//unsure if this works... maybe type of a type will return a Type type?
            {
                if (at == null || !AcceptBuilding_Casket((Building_Casket)at, t))
                {
                    return StoragePriority.Unstored;
                }
            }
            if (at.GetType() == typeof(Building_GibbetCage))
            {
                if (at == null || !AcceptBuilding_GibbetCage((Building_GibbetCage)at, t))
                {
                    return StoragePriority.Unstored;
                }
            }
            if (at.GetType() == typeof(Building_Grave))
            {
                if (at == null || !AcceptBuilding_Grave((Building_Grave)at, t))
                {
                    return StoragePriority.Unstored;
                }
            }
            if (at.GetType() == typeof(Building_Storage))
            {
                if (at == null || !AcceptBuilding_Storage((Building_Storage)at, t))
                {
                    return StoragePriority.Unstored;
                }
            }
            if (at.GetType() == typeof(Zone_Stockpile))
            {
                if (at == null || !AcceptBuilding_Zone_Stockpile((Zone_Stockpile)at, t))
                {
                    return StoragePriority.Unstored;
                }
            }
            return at.GetStoreSettings().Priority;//Possibly to finish

        }

        /// <summary>
        /// This method is implementing Building_Casket.Accept but works internally to the entity cache specifically for Building_Casket.
        /// </summary>
        public bool AcceptBuilding_Casket(Building_Casket at, int i)
        {
            return CanAcceptAnyOfIndex(i,at.innerContainer);//Obviously "this" in this context is at.
        }
        /// <summary>
        /// This method is implementing Building_GibbetCage.Accept but works internally to the entity cache specifically for Building_GibbetCage.
        /// </summary>
        public bool AcceptBuilding_GibbetCage(Building_GibbetCage at, int i)
        {
            return AcceptBuilding_Casket((Building_Casket)at, i) && !at.HasCorpse && !AllowedToAcceptIndex(i, at.storageSettings);
        }
        /// <summary>
        /// This method is implementing Building_Grave.Accept but works internally to the entity cache specifically for Building_Grave.
        /// </summary>
        public bool AcceptBuilding_Grave(Building_Grave at, int i)
        {
            if (!AcceptBuilding_Casket((Building_Casket)at, i) || at.HasCorpse)
                return false;
            if (at.AssignedPawn != null)
            {
                if (!(IndexToEntity[i] is Corpse corpse2) || corpse2.InnerPawn != at.AssignedPawn)
                    return false;
            }
            else if (!AllowedToAcceptIndex(i, at.storageSettings))
                return false;
            return true;
        }
        /// <summary>
        /// This method is implementing Building_Storage.Accept but works internally to the entity cache specifically for Building_Storage.
        /// </summary>
        public bool AcceptBuilding_Storage(Building_Storage at, int i)
        {
            return AllowedToAcceptIndex(i, at.settings);
        }
        /// <summary>
        /// This method is implementing Zone_Stockpile.Accept but works internally to the entity cache specifically for Zone_Stockpile.
        /// </summary>
        public bool AcceptBuilding_Zone_Stockpile(Zone_Stockpile at, int i)
        {
            return AllowedToAcceptIndex(i, at.settings);
        }
        /// <summary>
        /// This method is implementing ThingOwner.CanAcceptAnyOfIndex but works internally to the entity cache.
        /// </summary>
        public bool CanAcceptAnyOfIndex(int i, ThingOwner o, bool canMergeWithExistingStacks = true)
        {
            return GetCountCanAcceptIndex(i, o, canMergeWithExistingStacks) > 0;
        }
        /// <summary>
        /// This method is implementing StorageSettings.AllowedToAccept but works internally to the entity cache.
        /// </summary>
        public bool AllowedToAcceptIndex(int i, StorageSettings s)
        {
            if (!AllowsIndex(i, s.filter))
            {
                return false;
            }
            if (s.owner != null)
            {
                StorageSettings parentStoreSettings = s.owner.GetParentStoreSettings();
                if (parentStoreSettings != null && !AllowedToAcceptIndex(i, parentStoreSettings))//It's actually not that bad.
                {
                    return false;
                }
            }
            return true;
        }
        /// <summary>
        /// An iterative version of the StorageSettings.AllowedToAccept method but works internally to the entity cache. To Test.
        /// </summary>
        public bool AllowedToAcceptIndexIterative1(int i, StorageSettings s)
        {
            while ((s != null) && (s.owner != null) && AllowsIndex(i, s.filter))//optimized using circuit evaluation. Seems to be the best one.
            {
                s = s.owner.GetParentStoreSettings();
            }
            if (s == null)
            {
                return true;
            }
            if (!AllowsIndex(i, s.filter))
            {
                return false;
            }
            return true;
        }
        /// <summary>
        /// An iterative version of the StorageSettings.AllowedToAccept method but works internally to the entity cache.
        /// </summary>
        public bool AllowedToAcceptIndexIterativeStack(int i, StorageSettings s)
        {
            Stack<StorageSettings> IteratorStack = new Stack<StorageSettings>();
            IteratorStack.Push(s);
            while (IteratorStack.Count > 0)
            {
                s = IteratorStack.Pop();
                if (!AllowsIndex(i, s.filter))
                {
                    return false;
                }
                if (s.owner != null)
                {
                    StorageSettings parentStoreSettings = s.owner.GetParentStoreSettings();
                    if (parentStoreSettings != null)
                    {
                        IteratorStack.Push(parentStoreSettings);
                    }
                }
            }
            return true;
        }
        /// <summary>
        /// An iterative version of the StorageSettings.AllowedToAccept method but works internally to the entity cache and doesn't do any allocations in the managed heap.
        /// Everything about this method is doubtful WIP.
        /// </summary>
        public bool AllowedToAcceptIndexIterativeNoAllocs(int i, StorageSettings s)
        {
            Span<FakeStack> Stack = stackalloc FakeStack[1];
            Stack[1].s = EntityToIndex[(Thing)s.owner];//this is very doubtful. So doubtful infact I'd bet is wrong
            Stack[1].IsEmpty = false;
            while (!Stack[1].IsEmpty)
            {
                s = ((IStoreSettingsParent)IndexToEntity[Stack[1].s]).GetStoreSettings();
                Stack[1].IsEmpty = true;
                if (!AllowsIndex(i, s.filter))
                {
                    return false;
                }
                if (s.owner != null)
                {
                    StorageSettings parentStoreSettings = s.owner.GetParentStoreSettings();
                    if (parentStoreSettings != null)
                    {
                        Stack[1].s = EntityToIndex[(Thing)parentStoreSettings.owner];
                        Stack[1].IsEmpty = false;
                    }
                }
            }
            return true;
        }
        /// <summary>
        /// This method is implementing ThingOwner.GetCountCanAcceptIndex but works internally to the entity cache.
        /// </summary>
        public int GetCountCanAcceptIndex(int i, ThingOwner o, bool canMergeWithExistingStacks = true)
        {
            if (!IndexToEntity.ContainsKey(i) || stackCount[i] <= 0)
            {
                return 0;
            }
            if (o.maxStacks == 999999)
            {
                return stackCount[i];
            }
            int a = 0;
            if (o.Count < o.maxStacks)
            {
                a += (o.maxStacks - o.Count) * stackLimit[def[i]];
            }
            if (a >= stackCount[i] || !canMergeWithExistingStacks)
            {
                return Mathf.Min(a, stackCount[i]);
            }
            int index = 0;
            for (int count = o.Count; index < count; ++index)
            {
                int at = EntityToIndex[o.GetAt(index)];
                if (stackCount[at] < stackLimit[def[at]] && CanStackWithIndex( at, i))
                {
                    a += stackLimit[def[at]] - stackCount[at];
                    if (a >= stackCount[i])
                    {
                        return Mathf.Min(a, stackCount[i]);
                    }
                }
            }
            return Mathf.Min(a, stackCount[i]);
        }
        /// <summary>
        /// This method is implementing ThingFilter.Allows but works internally to the entity cache.
        /// </summary>
        public bool AllowsIndex(int i, ThingFilter f)
        {
            i = EntityToIndex[IndexToEntity[i].GetInnerIfMinified()];//To Finish. For now this can remains like this... changing GetInnerIfMinified would probaby require a postfix to the setter in MinifiedThing.InnerThing.
            if (!f.Allows(def[i]) || def[i].useHitPoints && !f.allowedHitPointsPercents.IncludesEpsilon(Mathf.Clamp01(GenMath.RoundedHundredth((float)HitPoints[i] / (float)MaxHitPoints[i]))))// can cache usehitpoints
            {
                return false;
            }
            if (f.allowedQualities != QualityRange.All && def[i].FollowQualityThingFilter())
            {
                QualityCategory qc;
                if (!IndexToEntity[i].TryGetQuality(out qc))//to finish
                {
                    qc = QualityCategory.Normal;
                }
                if (!f.allowedQualities.Includes(qc))
                {
                    return false;
                }
            }
            for (int index = 0; index < f.disallowedSpecialFilters.Count; ++index)
            {
                if (f.disallowedSpecialFilters[index].Worker.Matches(IndexToEntity[i]) && def[i].IsWithinCategory(f.disallowedSpecialFilters[index].parentCategory))
                {
                    return false;
                }
            }
            return true;
        }
        /// <summary>
        /// This method is implementing Thing.CanStackWith but works internally to the entity cache.
        /// </summary>
        public bool CanStackWithIndex(int at, int i)
        {
            return !DestroyedIndex(at) && !DestroyedIndex(i) && (def[at].category == ThingCategory.Item && def[at] == def[i]) && StuffInt[at] == StuffInt[i];
        }
        /// <summary>
        /// This method is implementing Thing.Destroyed but works internally to the entity cache.
        /// </summary>
        public bool DestroyedIndex(int i)
        {
            return mapIndexOrState[i] == (sbyte) -2 || mapIndexOrState[i] == (sbyte) -3;
        }
        /// <summary>
        /// This method is implementing Thing.TryGetQuality but works internally to the entity cache.
        /// </summary>
        public bool TryGetQualityIndex(int i, out QualityCategory qc)//doubts about what spawn despawn minifiedThing uses.
        {
            CompQuality compQuality = IndexToEntity[i] is MinifiedThing minifiedThing ? minifiedThing.InnerThing.TryGetComp<CompQuality>() : IndexToEntity[i].TryGetComp<CompQuality>();// To Finish. I ll stop here for now.
            if (compQuality == null)
            {
                qc = QualityCategory.Normal;
                return false;
            }
            qc = compQuality.Quality;
            return true;
        }
    }
}

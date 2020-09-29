#region Assembly Assembly-CSharp, Version=1.2.7558.21380, Culture=neutral, PublicKeyToken=null
// C:\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed\Assembly-CSharp.dll
// Decompiled with ICSharpCode.Decompiler 5.0.2.5153
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Verse
{
    public class ThingOwner_Target<T> : ThingOwner, IList<T>, ICollection<T>, IEnumerable<T>, IEnumerable where T : Thing
    {
        private List<T> innerList = new List<T>();

        public List<T> InnerListForReading => innerList;

        public new T this[int index] => innerList[index];

        public override int Count => innerList.Count;

        T IList<T>.this[int index]
        {
            get
            {
                return innerList[index];
            }
            set
            {
                throw new InvalidOperationException("ThingOwner doesn't allow setting individual elements.");
            }
        }

        bool ICollection<T>.IsReadOnly => true;

        public ThingOwner_Target()
        {
        }

        public ThingOwner_Target(IThingHolder owner)
            : base(owner)
        {
        }

        public ThingOwner_Target(IThingHolder owner, bool oneStackOnly, LookMode contentsLookMode = LookMode.Deep)
            : base(owner, oneStackOnly, contentsLookMode)
        {
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref innerList, true, "innerList", contentsLookMode);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                innerList.RemoveAll((T x) => x == null);
            }

            if (Scribe.mode != LoadSaveMode.LoadingVars && Scribe.mode != LoadSaveMode.PostLoadInit)
            {
                return;
            }

            for (int i = 0; i < innerList.Count; i++)
            {
                if (innerList[i] != null)
                {
                    innerList[i].holdingOwner = this;
                }
            }
        }

        public List<T>.Enumerator GetEnumerator()
        {
            return innerList.GetEnumerator();
        }

        public override int GetCountCanAccept(Thing item, bool canMergeWithExistingStacks = true)
        {
            if (!(item is T))
            {
                return 0;
            }

            return base.GetCountCanAccept(item, canMergeWithExistingStacks);
        }

        public override int TryAdd(Thing item, int count, bool canMergeWithExistingStacks = true)
        {
            if (count <= 0)
            {
                return 0;
            }

            if (item == null)
            {
                Log.Warning("Tried to add null item to ThingOwner.");
                return 0;
            }

            if (Contains(item))
            {
                Log.Warning("Tried to add " + item + " to ThingOwner but this item is already here.");
                return 0;
            }

            if (item.holdingOwner != null)
            {
                Log.Warning("Tried to add " + count + " of " + item.ToStringSafe() + " to ThingOwner but this thing is already in another container. owner=" + owner.ToStringSafe() + ", current container owner=" + item.holdingOwner.Owner.ToStringSafe() + ". Use TryAddOrTransfer, TryTransferToContainer, or remove the item before adding it.");
                return 0;
            }

            if (!CanAcceptAnyOf(item, canMergeWithExistingStacks))
            {
                return 0;
            }

            int stackCount = item.stackCount;
            int num = Mathf.Min(stackCount, count);
            Thing thing = item.SplitOff(num);
            if (!TryAdd((T)thing, canMergeWithExistingStacks))
            {
                if (thing != item)
                {
                    int result = stackCount - item.stackCount - thing.stackCount;
                    item.TryAbsorbStack(thing, respectStackLimit: false);
                    return result;
                }

                return stackCount - item.stackCount;
            }

            return num;
        }

        public override bool TryAdd(Thing item, bool canMergeWithExistingStacks = true)
        {
            if (item == null)
            {
                Log.Warning("Tried to add null item to ThingOwner.");
                return false;
            }

            T val = item as T;
            if (val == null)
            {
                return false;
            }

            if (Contains(item))
            {
                Log.Warning("Tried to add " + item.ToStringSafe() + " to ThingOwner but this item is already here.");
                return false;
            }

            if (item.holdingOwner != null)
            {
                Log.Warning("Tried to add " + item.ToStringSafe() + " to ThingOwner but this thing is already in another container. owner=" + owner.ToStringSafe() + ", current container owner=" + item.holdingOwner.Owner.ToStringSafe() + ". Use TryAddOrTransfer, TryTransferToContainer, or remove the item before adding it.");
                return false;
            }

            if (!CanAcceptAnyOf(item, canMergeWithExistingStacks))
            {
                return false;
            }

            if (canMergeWithExistingStacks)
            {
                for (int i = 0; i < innerList.Count; i++)
                {
                    T val2 = innerList[i];
                    if (!val2.CanStackWith(item))
                    {
                        continue;
                    }

                    int num = Mathf.Min(item.stackCount, val2.def.stackLimit - val2.stackCount);
                    if (num > 0)
                    {
                        Thing other = item.SplitOff(num);
                        int stackCount = val2.stackCount;
                        val2.TryAbsorbStack(other, respectStackLimit: true);
                        if (val2.stackCount > stackCount)
                        {
                            NotifyAddedAndMergedWith(val2, val2.stackCount - stackCount);
                        }

                        if (item.Destroyed || item.stackCount == 0)
                        {
                            return true;
                        }
                    }
                }
            }

            if (Count >= maxStacks)
            {
                return false;
            }

            item.holdingOwner = this;
            lock(innerList) {
                innerList.Add(val);
            }
            NotifyAdded(val);
            return true;
        }

        public void TryAddRangeOrTransfer(IEnumerable<T> things, bool canMergeWithExistingStacks = true, bool destroyLeftover = false)
        {
            if (things == this)
            {
                return;
            }

            ThingOwner thingOwner = things as ThingOwner;
            if (thingOwner != null)
            {
                thingOwner.TryTransferAllToContainer(this, canMergeWithExistingStacks);
                if (destroyLeftover)
                {
                    thingOwner.ClearAndDestroyContents();
                }

                return;
            }

            IList<T> list = things as IList<T>;
            if (list != null)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    if (!TryAddOrTransfer(list[i], canMergeWithExistingStacks) && destroyLeftover)
                    {
                        list[i].Destroy();
                    }
                }
            }
            else
            {
                foreach (T thing in things)
                {
                    if (!TryAddOrTransfer(thing, canMergeWithExistingStacks) && destroyLeftover)
                    {
                        thing.Destroy();
                    }
                }
            }
        }

        public override int IndexOf(Thing item)
        {
            T val = item as T;
            if (val == null)
            {
                return -1;
            }

            return innerList.IndexOf(val);
        }

        public override bool Remove(Thing item)
        {
            if (!Contains(item))
            {
                return false;
            }

            if (item.holdingOwner == this)
            {
                item.holdingOwner = null;
            }
            lock (innerList)
            {
                int index = innerList.LastIndexOf((T)item);
                if(index == -1)
                    return false;
                innerList.RemoveAt(index);
            }
            NotifyRemoved(item);
            return true;
        }

        public int RemoveAll(Predicate<T> predicate)
        {
            int num = 0;
            for (int num2 = innerList.Count - 1; num2 >= 0; num2--)
            {
                if (predicate(innerList[num2]))
                {
                    Remove(innerList[num2]);
                    num++;
                }
            }

            return num;
        }

        protected override Thing GetAt(int index)
        {
            return innerList[index];
        }

        public int TryTransferToContainer(Thing item, ThingOwner otherContainer, int stackCount, out T resultingTransferredItem, bool canMergeWithExistingStacks = true)
        {
            Thing resultingTransferredItem2;
            int result = TryTransferToContainer(item, otherContainer, stackCount, out resultingTransferredItem2, canMergeWithExistingStacks);
            resultingTransferredItem = (T)resultingTransferredItem2;
            return result;
        }

        public new T Take(Thing thing, int count)
        {
            return (T)base.Take(thing, count);
        }

        public new T Take(Thing thing)
        {
            return (T)base.Take(thing);
        }

        public bool TryDrop(Thing thing, IntVec3 dropLoc, Map map, ThingPlaceMode mode, int count, out T resultingThing, Action<T, int> placedAction = null, Predicate<IntVec3> nearPlaceValidator = null)
        {
            Action<Thing, int> placedAction2 = null;
            if (placedAction != null)
            {
                placedAction2 = delegate (Thing t, int c)
                {
                    placedAction((T)t, c);
                };
            }

            Thing resultingThing2;
            bool result = TryDrop(thing, dropLoc, map, mode, count, out resultingThing2, placedAction2, nearPlaceValidator);
            resultingThing = (T)resultingThing2;
            return result;
        }

        public bool TryDrop(Thing thing, ThingPlaceMode mode, out T lastResultingThing, Action<T, int> placedAction = null, Predicate<IntVec3> nearPlaceValidator = null)
        {
            Action<Thing, int> placedAction2 = null;
            if (placedAction != null)
            {
                placedAction2 = delegate (Thing t, int c)
                {
                    placedAction((T)t, c);
                };
            }

            Thing lastResultingThing2;
            bool result = TryDrop(thing, mode, out lastResultingThing2, placedAction2, nearPlaceValidator);
            lastResultingThing = (T)lastResultingThing2;
            return result;
        }

        public bool TryDrop(Thing thing, IntVec3 dropLoc, Map map, ThingPlaceMode mode, out T lastResultingThing, Action<T, int> placedAction = null, Predicate<IntVec3> nearPlaceValidator = null)
        {
            Action<Thing, int> placedAction2 = null;
            if (placedAction != null)
            {
                placedAction2 = delegate (Thing t, int c)
                {
                    placedAction((T)t, c);
                };
            }

            Thing lastResultingThing2;
            bool result = TryDrop_NewTmp(thing, dropLoc, map, mode, out lastResultingThing2, placedAction2, nearPlaceValidator);
            lastResultingThing = (T)lastResultingThing2;
            return result;
        }

        int IList<T>.IndexOf(T item)
        {
            return innerList.IndexOf(item);
        }

        void IList<T>.Insert(int index, T item)
        {
            throw new InvalidOperationException("ThingOwner doesn't allow inserting individual elements at any position.");
        }

        void ICollection<T>.Add(T item)
        {
            TryAdd(item);
        }

        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            innerList.CopyTo(array, arrayIndex);
        }

        bool ICollection<T>.Contains(T item)
        {
            return innerList.Contains(item);
        }

        bool ICollection<T>.Remove(T item)
        {
            return Remove(item);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return innerList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return innerList.GetEnumerator();
        }
    }
}
#if false // Decompilation log
'17' items in cache
------------------
Resolve: 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Found single assembly: 'mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\mscorlib.dll'
------------------
Resolve: 'NAudio, Version=1.7.3.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'NAudio, Version=1.7.3.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'NVorbis, Version=0.8.4.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'NVorbis, Version=0.8.4.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'UnityEngine.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'UnityEngine.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed\UnityEngine.CoreModule.dll'
------------------
Resolve: 'UnityEngine.AudioModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'UnityEngine.AudioModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed\UnityEngine.AudioModule.dll'
------------------
Resolve: 'System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Found single assembly: 'System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\System.dll'
------------------
Resolve: 'System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Found single assembly: 'System.Core, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\System.Core.dll'
------------------
Resolve: 'UnityEngine.IMGUIModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'UnityEngine.IMGUIModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed\UnityEngine.IMGUIModule.dll'
------------------
Resolve: 'Assembly-CSharp-firstpass, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'Assembly-CSharp-firstpass, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'System.Xml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Found single assembly: 'System.Xml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\System.Xml.dll'
------------------
Resolve: 'System.Xml.Linq, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Found single assembly: 'System.Xml.Linq, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089'
Load from: 'C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\System.Xml.Linq.dll'
------------------
Resolve: 'UnityEngine.AssetBundleModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Found single assembly: 'UnityEngine.AssetBundleModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Load from: 'C:\Steam\steamapps\common\RimWorld\RimWorldWin64_Data\Managed\UnityEngine.AssetBundleModule.dll'
------------------
Resolve: 'UnityEngine.TextRenderingModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'UnityEngine.TextRenderingModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'UnityEngine.PhysicsModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'UnityEngine.PhysicsModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'Unity.TextMeshPro, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'Unity.TextMeshPro, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'ISharpZipLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'ISharpZipLib, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'UnityEngine.InputLegacyModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'UnityEngine.InputLegacyModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'UnityEngine.PerformanceReportingModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'UnityEngine.PerformanceReportingModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'UnityEngine.ImageConversionModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'UnityEngine.ImageConversionModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'UnityEngine.ScreenCaptureModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'UnityEngine.ScreenCaptureModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null'
------------------
Resolve: 'UnityEngine.UI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
Could not find by name: 'UnityEngine.UI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null'
#endif

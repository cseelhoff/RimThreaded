using RimWorld.Planet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Xml;
using Verse;

namespace RimThreaded
{
    public class ThreadSafeLinkedList<T> : List<T>
    {
#if DEBUG
        SpinLock spinLock = new SpinLock(true);
#else
        SpinLock spinLock = new SpinLock(false);
#endif

        public static bool warningIssuedIndex;
        public static bool warningIssuedRemove;
        private int itemCount;
        private ThreadSafeNode<T> firstNode;
        private ThreadSafeNode<T> lastNode;

        public ThreadSafeLinkedList(int capacity) : base(capacity)
        {
        }

        public ThreadSafeLinkedList()
        {
        }
        //int ICollection<T>.Count => itemCount;
        public new int Count
        {
            get
            {
                return itemCount;
            }
        }

        public bool IsReadOnly => false;

        public new T this[int index]
        {
            get
            {
                return getNodeAtIndex(index).value;
            }
            set
            {
                getNodeAtIndex(index).value = value;
            }
        }
        ThreadSafeNode<T> getNodeAtIndex(int index)
        {
            if (!warningIssuedIndex)
            {
                Log.Warning("Using an index on ThreadSafeLinkedList will lead to errors!");
                warningIssuedIndex = true;
            }
            bool lockTaken = false;
            int i = 0;
            try
            {
                foreach (ThreadSafeNode<T> threadSafeNode in this)
                {
                    if (i == index)
                    {
                        return threadSafeNode;
                    }
                    i++;
                }
            }
            finally
            {
                if (lockTaken) spinLock.Exit();
            }
            Log.Error("a:" + i + " b:" + index + " c:" + itemCount);
            throw new ArgumentOutOfRangeException();
        }

        public void Add(ThreadSafeNode<T> threadSafeNode)
        {
            bool lockTaken = false;
            try
            {
                spinLock.Enter(ref lockTaken);
                if (firstNode == null)
                {
                    firstNode = threadSafeNode;
                }
                else
                {
                    threadSafeNode.previousNode = lastNode;
                    lastNode.nextNode = threadSafeNode;
                }
                lastNode = threadSafeNode;
                itemCount++;
            }
            finally
            {
                if (lockTaken) spinLock.Exit();
            }
        }
        public new void Add(T obj)
        {
            Add(new ThreadSafeNode<T>(obj));
        }
        public void InsertAfter(ThreadSafeNode<T> previousNode, ThreadSafeNode<T> newNode)
        {
            bool lockTaken = false;
            try
            {
                spinLock.Enter(ref lockTaken);
                newNode.nextNode = previousNode.nextNode;
                newNode.previousNode = previousNode;
                previousNode.nextNode = newNode;
                if (lastNode == previousNode)
                {
                    lastNode = newNode;
                }
                itemCount++;
            }
            finally
            {
                if (lockTaken) spinLock.Exit();
            }
        }
        public void InsertBefore(ThreadSafeNode<T> nextNode, ThreadSafeNode<T> newNode)
        {
            bool lockTaken = false;
            try
            {
                spinLock.Enter(ref lockTaken);
                newNode.nextNode = nextNode;
                newNode.previousNode = nextNode.previousNode;
                nextNode.previousNode = newNode;
                if (firstNode == nextNode)
                {
                    firstNode = newNode;
                }
                itemCount++;
            }
            finally
            {
                if (lockTaken) spinLock.Exit();
            }
        }
        public new void Insert(int index, T obj)
        {
            Insert(index, new ThreadSafeNode<T>(obj));
        }

        public void Insert(int index, ThreadSafeNode<T> newNode)
        {
            bool lockTaken = false;
            try
            {
                spinLock.Enter(ref lockTaken);
                if (itemCount == 0)
                {
                    firstNode = newNode;
                    lastNode = newNode;
                }
                else
                {
                    InsertBefore(getNodeAtIndex(index), newNode);
                }
            }
            finally
            {
                if (lockTaken) spinLock.Exit();
            }
        }
        public bool Remove(ThreadSafeNode<T> threadSafeNode)
        {
            bool lockTaken = false;
            try
            {
                spinLock.Enter(ref lockTaken);
                itemCount--;
                if (threadSafeNode == firstNode)
                {
                    firstNode = threadSafeNode.nextNode;
                }
                if (threadSafeNode == lastNode)
                {
                    lastNode = threadSafeNode.previousNode;
                }
                if (threadSafeNode.previousNode != null)
                {
                    threadSafeNode.previousNode.nextNode = threadSafeNode.nextNode;
                }
                if (threadSafeNode.nextNode != null)
                {
                    threadSafeNode.nextNode.previousNode = threadSafeNode.previousNode;
                }
            }
            finally
            {
                if (lockTaken) spinLock.Exit();
            }
            return true;
        }

        public new int IndexOf(T item)
        {
            if (!warningIssuedIndex)
            {
                Log.Warning("Using an index on ThreadSafeLinkedList will lead to errors!");
                warningIssuedIndex = true;
            }
            int i = 0;
            foreach (ThreadSafeNode<T> threadSafeNode in this)
            {
                if (threadSafeNode.value.Equals(item))
                {
                    return i;
                }
                i++;
            }
            return -1;
        }

        public new void RemoveAt(int index)
        {
            bool lockTaken = false;
            try
            {
                Remove(getNodeAtIndex(index));
            }
            finally
            {
                if (lockTaken) spinLock.Exit();
            }
            return;
        }

        public new bool Remove(T item)
        {
            if (!warningIssuedRemove)
            {
                Log.Warning("Calling ThreadSafeLinkedList.Remove(T item) is not optimal and will remove the first occurance. ThreadSafeLinkedList.Remove(T item) is preferred.");
                warningIssuedRemove = true;
            }
            bool lockTaken = false;
            try
            {
                spinLock.Enter(ref lockTaken);
                foreach (ThreadSafeNode<T> threadSafeNode in this)
                {
                    if (threadSafeNode.value.Equals(item))
                    {
                        itemCount--;
                        Remove(threadSafeNode);
                    }
                }
            }
            finally
            {
                if (lockTaken) spinLock.Exit();
            }
            return true;
        }
        public new void Clear()
        {
            bool lockTaken = false;
            try
            {
                spinLock.Enter(ref lockTaken);
                itemCount = 0;
                firstNode = null;
                lastNode = null;
            }
            finally
            {
                if (lockTaken) spinLock.Exit();
            }
        }

        public new bool Contains(T item)
        {
            if (!warningIssuedRemove)
            {
                Log.Warning("Calling ThreadSafeLinkedList.Contains(T item) is not optimal. ThreadSafeLinkedList.getFirstNodeContaining(T item) or writing a custom Enumeration loop is preferred.");
                warningIssuedRemove = true;
            }
            foreach (ThreadSafeNode<T> threadSafeNode in this)
            {
                if (threadSafeNode.value.Equals(item))
                {
                    return true;
                }
            }
            return false;
        }
        public ThreadSafeNode<T> getFirstNodeContaining(T item)
        {
            foreach (ThreadSafeNode<T> threadSafeNode in this)
            {
                if (threadSafeNode.value.Equals(item))
                {
                    return threadSafeNode;
                }
            }
            return null;
        }

        public bool Contains(ThreadSafeNode<T> node)
        {
            foreach (ThreadSafeNode<T> threadSafeNode in this)
            {
                if (threadSafeNode.Equals(node))
                {
                    return true;
                }
            }
            return false;
        }

        public new void CopyTo(T[] array, int arrayIndex)
        {
            int i = 0;
            foreach (ThreadSafeNode<T> threadSafeNode in this)
            {
                if (i >= arrayIndex)
                    array[i - arrayIndex] = threadSafeNode.value;
                i++;
            };
        }

        public new IEnumerator GetEnumerator()
        {
            return new Enumerator(this);
        }
        //IEnumerator IEnumerable.GetEnumerator()
        //{
        //    return new Enumerator(this);
        //}


        //IEnumerator<T> IEnumerable<T>.GetEnumerator()
        //{
        //    return new Enumerator(this);
        //}

        public new class Enumerator : IEnumerator<T>, IDisposable, IEnumerator
        {
            private readonly ThreadSafeLinkedList<T> list;
            private ThreadSafeNode<T> currentNode;

            public ThreadSafeNode<T> Current
            {
                get
                {
                    if (currentNode == null) throw new InvalidOperationException("Enumerator Ended");

                    return currentNode;
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    if (currentNode == null) throw new InvalidOperationException("Enumerator Ended");
                    return currentNode;
                }
            }

            T IEnumerator<T>.Current => Current.value;

            public void Dispose()
            {
                currentNode = null;
            }

            public bool MoveNext()
            {
                currentNode = currentNode?.nextNode;
                return currentNode != null;
            }

            public void Reset()
            {
                currentNode = list.firstNode;
            }
            internal Enumerator(ThreadSafeLinkedList<T> threadSafeLinkedList)
            {
                list = threadSafeLinkedList;
                currentNode = threadSafeLinkedList.firstNode;
            }
        }

    }

    public class ThreadSafeNode<T>
    {
        public ThreadSafeNode<T> previousNode;
        public ThreadSafeNode<T> nextNode;
        public T value;

        public T Obj { get; }
        public ThreadSafeNode<T> NextNode { get; }

        public ThreadSafeNode(T obj, ThreadSafeNode<T> previous)
        {
            value = obj;
            previousNode = previous;
        }
        public ThreadSafeNode(T obj)
        {
            value = obj;
        }

        public ThreadSafeNode(T obj, ThreadSafeNode<T> previous, ThreadSafeNode<T> next)
        {
            value = obj;
            previousNode = previous;
            nextNode = next;
        }
    }
    public class Scribe_Collections_Patch
    {
        public static void Look<T>(ref ThreadSafeLinkedList<T> list, string label, LookMode lookMode = LookMode.Undefined, params object[] ctorArgs)
        {
            Look(ref list, saveDestroyedThings: false, label, lookMode, ctorArgs);
        }

        public static void Look<T>(ref ThreadSafeLinkedList<T> list, bool saveDestroyedThings, string label, LookMode lookMode = LookMode.Undefined, params object[] ctorArgs)
        {
            if (lookMode == LookMode.Undefined && !Scribe_Universal.TryResolveLookMode(typeof(T), out lookMode))
            {
                Log.Error(string.Concat("LookList call with a list of ", typeof(T), " must have lookMode set explicitly."));
            }
            else if (Scribe.EnterNode(label))
            {
                try
                {
                    if (Scribe.mode == LoadSaveMode.Saving)
                    {
                        if (list == null)
                        {
                            Scribe.saver.WriteAttribute("IsNull", "True");
                            return;
                        }
                        foreach (T item8 in list)
                        {
                            switch (lookMode)
                            {
                                case LookMode.Value:
                                    {
                                        T value5 = item8;
                                        Scribe_Values.Look(ref value5, "li", default(T), forceSave: true);
                                        break;
                                    }
                                case LookMode.LocalTargetInfo:
                                    {
                                        LocalTargetInfo value4 = (LocalTargetInfo)(object)item8;
                                        Scribe_TargetInfo.Look(ref value4, saveDestroyedThings, "li");
                                        break;
                                    }
                                case LookMode.TargetInfo:
                                    {
                                        TargetInfo value3 = (TargetInfo)(object)item8;
                                        Scribe_TargetInfo.Look(ref value3, saveDestroyedThings, "li");
                                        break;
                                    }
                                case LookMode.GlobalTargetInfo:
                                    {
                                        GlobalTargetInfo value2 = (GlobalTargetInfo)(object)item8;
                                        Scribe_TargetInfo.Look(ref value2, saveDestroyedThings, "li");
                                        break;
                                    }
                                case LookMode.Def:
                                    {
                                        Def value = (Def)(object)item8;
                                        Scribe_Defs.Look(ref value, "li");
                                        break;
                                    }
                                case LookMode.BodyPart:
                                    {
                                        BodyPartRecord part = (BodyPartRecord)(object)item8;
                                        Scribe_BodyParts.Look(ref part, "li");
                                        break;
                                    }
                                case LookMode.Deep:
                                    {
                                        T target = item8;
                                        Scribe_Deep.Look(ref target, saveDestroyedThings, "li", ctorArgs);
                                        break;
                                    }
                                case LookMode.Reference:
                                    {
                                        ILoadReferenceable refee = (ILoadReferenceable)(object)item8;
                                        Scribe_References.Look(ref refee, "li", saveDestroyedThings);
                                        break;
                                    }
                            }
                        }
                    }
                    else if (Scribe.mode == LoadSaveMode.LoadingVars)
                    {
                        XmlNode curXmlParent = Scribe.loader.curXmlParent;
                        XmlAttribute xmlAttribute = curXmlParent.Attributes["IsNull"];
                        if (xmlAttribute != null && xmlAttribute.Value.ToLower() == "true")
                        {
                            if (lookMode == LookMode.Reference)
                            {
                                Scribe.loader.crossRefs.loadIDs.RegisterLoadIDListReadFromXml(null, null);
                            }
                            list = null;
                            return;
                        }
                        switch (lookMode)
                        {
                            case LookMode.Value:
                                list = new ThreadSafeLinkedList<T>(curXmlParent.ChildNodes.Count);
                                foreach (XmlNode childNode in curXmlParent.ChildNodes)
                                {
                                    T item = ScribeExtractor.ValueFromNode(childNode, default(T));
                                    list.Add(item);
                                }
                                break;
                            case LookMode.Deep:
                                list = new ThreadSafeLinkedList<T>(curXmlParent.ChildNodes.Count);
                                foreach (XmlNode childNode2 in curXmlParent.ChildNodes)
                                {
                                    T item7 = ScribeExtractor.SaveableFromNode<T>(childNode2, ctorArgs);
                                    list.Add(item7);
                                }
                                break;
                            case LookMode.Def:
                                list = new ThreadSafeLinkedList<T>(curXmlParent.ChildNodes.Count);
                                foreach (XmlNode childNode3 in curXmlParent.ChildNodes)
                                {
                                    T item6 = ScribeExtractor.DefFromNodeUnsafe<T>(childNode3);
                                    list.Add(item6);
                                }
                                break;
                            case LookMode.BodyPart:
                                {
                                    list = new ThreadSafeLinkedList<T>(curXmlParent.ChildNodes.Count);
                                    int num4 = 0;
                                    foreach (XmlNode childNode4 in curXmlParent.ChildNodes)
                                    {
                                        T item5 = (T)(object)ScribeExtractor.BodyPartFromNode(childNode4, num4.ToString(), null);
                                        list.Add(item5);
                                        num4++;
                                    }
                                    break;
                                }
                            case LookMode.LocalTargetInfo:
                                {
                                    list = new ThreadSafeLinkedList<T>(curXmlParent.ChildNodes.Count);
                                    int num3 = 0;
                                    foreach (XmlNode childNode5 in curXmlParent.ChildNodes)
                                    {
                                        T item4 = (T)(object)ScribeExtractor.LocalTargetInfoFromNode(childNode5, num3.ToString(), LocalTargetInfo.Invalid);
                                        list.Add(item4);
                                        num3++;
                                    }
                                    break;
                                }
                            case LookMode.TargetInfo:
                                {
                                    list = new ThreadSafeLinkedList<T>(curXmlParent.ChildNodes.Count);
                                    int num2 = 0;
                                    foreach (XmlNode childNode6 in curXmlParent.ChildNodes)
                                    {
                                        T item3 = (T)(object)ScribeExtractor.TargetInfoFromNode(childNode6, num2.ToString(), TargetInfo.Invalid);
                                        list.Add(item3);
                                        num2++;
                                    }
                                    break;
                                }
                            case LookMode.GlobalTargetInfo:
                                {
                                    list = new ThreadSafeLinkedList<T>(curXmlParent.ChildNodes.Count);
                                    int num = 0;
                                    foreach (XmlNode childNode7 in curXmlParent.ChildNodes)
                                    {
                                        T item2 = (T)(object)ScribeExtractor.GlobalTargetInfoFromNode(childNode7, num.ToString(), GlobalTargetInfo.Invalid);
                                        list.Add(item2);
                                        num++;
                                    }
                                    break;
                                }
                            case LookMode.Reference:
                                {
                                    List<string> list2 = new List<string>(curXmlParent.ChildNodes.Count);
                                    foreach (XmlNode childNode8 in curXmlParent.ChildNodes)
                                    {
                                        list2.Add(childNode8.InnerText);
                                    }
                                    Scribe.loader.crossRefs.loadIDs.RegisterLoadIDListReadFromXml(list2, "");
                                    break;
                                }
                        }
                    }
                    else
                    {
                        if (Scribe.mode != LoadSaveMode.ResolvingCrossRefs)
                        {
                            return;
                        }
                        switch (lookMode)
                        {
                            case LookMode.Reference:
                                list = TakeResolvedRefList<T>(Scribe.loader.crossRefs, "");
                                break;
                            case LookMode.LocalTargetInfo:
                                if (list != null)
                                {
                                    for (int j = 0; j < list.Count; j++)
                                    {
                                        list[j] = (T)(object)ScribeExtractor.ResolveLocalTargetInfo((LocalTargetInfo)(object)list[j], j.ToString());
                                    }
                                }
                                break;
                            case LookMode.TargetInfo:
                                if (list != null)
                                {
                                    for (int k = 0; k < list.Count; k++)
                                    {
                                        list[k] = (T)(object)ScribeExtractor.ResolveTargetInfo((TargetInfo)(object)list[k], k.ToString());
                                    }
                                }
                                break;
                            case LookMode.GlobalTargetInfo:
                                if (list != null)
                                {
                                    for (int i = 0; i < list.Count; i++)
                                    {
                                        list[i] = (T)(object)ScribeExtractor.ResolveGlobalTargetInfo((GlobalTargetInfo)(object)list[i], i.ToString());
                                    }
                                }
                                break;
                        }
                        return;
                    }
                }
                finally
                {
                    Scribe.ExitNode();
                }
            }
            else if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                if (lookMode == LookMode.Reference)
                {
                    Scribe.loader.crossRefs.loadIDs.RegisterLoadIDListReadFromXml(null, label);
                }
                list = null;
            }
        }
        public static ThreadSafeLinkedList<T> TakeResolvedRefList<T>(CrossRefHandler __instance, string toAppendToPathRelToParent)
        {
            string text = Scribe.loader.curPathRelToParent;
            if (!toAppendToPathRelToParent.NullOrEmpty())
            {
                text = text + "/" + toAppendToPathRelToParent;
            }
            return TakeResolvedRefList<T>(__instance, text, Scribe.loader.curParent);
        }
        public static ThreadSafeLinkedList<T> TakeResolvedRefList<T>(CrossRefHandler __instance, string pathRelToParent, IExposable parent)
        {
            List<string> list = __instance.loadIDs.TakeList(pathRelToParent, parent);
            ThreadSafeLinkedList<T> list2 = new ThreadSafeLinkedList<T>();
            if (list != null)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    list2.Add(__instance.loadedObjectDirectory.ObjectWithLoadID<T>(list[i]));
                }
            }
            return list2;
        }

    }

}

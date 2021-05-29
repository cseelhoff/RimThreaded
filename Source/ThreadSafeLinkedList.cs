using System;
using System.Collections;
using System.Threading;

namespace RimThreaded
{
    public class ThreadSafeLinkedList<T> : IEnumerable
    {
        private int itemCount;
        private ThreadSafeNode<T> firstNode;
        private ThreadSafeNode<T> lastNode;
#if DEBUG
        SpinLock spinLock = new SpinLock(true);
#else
        SpinLock spinLock = new SpinLock(false);
#endif
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
                }
                lastNode = threadSafeNode;
                itemCount++;
            }
            finally
            {
                if (lockTaken) spinLock.Exit();
            }
        }
        public void Add(T obj)
        {
            bool lockTaken = false;
            try
            {
                spinLock.Enter(ref lockTaken);
                if (firstNode == null)
                {
                    firstNode = new ThreadSafeNode<T>(obj);
                    lastNode = firstNode;
                }
                else
                {
                    lastNode = new ThreadSafeNode<T>(obj, lastNode);
                }
                itemCount++;
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

        public int Count()
        {
            return itemCount;
        }

        public IEnumerator GetEnumerator()
        {
            return new Enumerator(this);
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }
        public class Enumerator : IEnumerator
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

            public void Dispose()
            {
                currentNode = null;
            }

            public bool MoveNext()
            {
                currentNode = currentNode.nextNode;
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

        public ThreadSafeNode(T obj, ThreadSafeNode<T> previous)
        {
            value = obj;
            previousNode = previous;
            previous.nextNode = this;
        }
        public ThreadSafeNode(T obj)
        {
            value = obj;
        }

    }
}

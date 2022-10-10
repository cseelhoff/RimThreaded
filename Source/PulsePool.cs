using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace RimThreaded
{
    public static class PulsePool<T> where T : new()
    {
        private static readonly ConcurrentQueue<T> FreeItems = new ConcurrentQueue<T>();

        public static int FreeItemsCount => FreeItems.Count;
        public static T Pulse(T o)
        {
            lock (FreeItems)
            {
                if (!FreeItems.Contains(o))
                {
                    FreeItems.Enqueue(o);
                }
                FreeItems.TryDequeue(out T freeItem);
                if (freeItem.Equals(o))
                {
                    ExpandPulsePool();
                    FreeItems.Enqueue(o);
                    return new T();
                }
                return freeItem;
            }
        }
        internal static void ExpandPulsePool()
        {
            for (int i = 0; i != 20; i++)
            {
                FreeItems.Enqueue(new T());
            }
        }
    }
}

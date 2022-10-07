using System.Collections.Concurrent;
using Verse;

namespace RimThreaded.RW_Patches
{
    public static class FullPool_Patch<T> where T : IFullPoolable, new()
    {
        private static readonly ConcurrentStack<T> FreeItems = new ConcurrentStack<T>();

        public static T Get()
        {
            return !FreeItems.TryPop(out T freeItem) ? new T() : freeItem;
        }

        public static void Return(T item)
        {
            item.Reset();
            FreeItems.Push(item);
        }
    }
}

using System.Collections.Concurrent;

namespace RimThreaded
{
    public static class SimplePool_Patch<T> where T : new()
    {
        private static readonly ConcurrentStack<T> FreeItems = new ConcurrentStack<T>();

        public static int FreeItemsCount => FreeItems.Count;

        public static T Get() => FreeItems.TryPop(out T freeItem) ? freeItem : new T();

        public static void Return(T item) => FreeItems.Push(item);
    }
}

using System.Collections.Concurrent;

namespace RimThreaded
{
    public static class SimplePool<T> where T : new()
    {
        private static readonly ConcurrentStack<T> FreeItems = new ConcurrentStack<T>();

        public static int FreeItemsCount => FreeItems.Count;

        public static T Get()
        {
            //int index = SimplePool<T>.freeItems.Count - 1;
            return !FreeItems.TryPop(out T freeItem) ? new T() : freeItem;
            //SimplePool<T>.freeItems.RemoveAt(index);
        }

        public static void Return(T item) => FreeItems.Push(item);
    }
}

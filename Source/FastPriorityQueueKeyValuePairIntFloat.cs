using System.Collections.Generic;

namespace RimThreaded
{
    public class FastPriorityQueueKeyValuePairIntFloat
    {
        protected List<KeyValuePair<int, float>> innerList = new List<KeyValuePair<int, float>>();

        protected IComparer<KeyValuePair<int, float>> comparer;

        public int Count => innerList.Count;

        public FastPriorityQueueKeyValuePairIntFloat(IComparer<KeyValuePair<int, float>> comparer)
        {
            this.comparer = comparer;
        }

        public void Push(KeyValuePair<int, float> item)
        {
            int num = innerList.Count;
            innerList.Add(item);
            while (num != 0)
            {
                int num2 = (num - 1) / 2;
                if (CompareElements(num, num2) < 0)
                {
                    SwapElements(num, num2);
                    num = num2;
                    continue;
                }

                break;
            }
        }

        public KeyValuePair<int, float> Pop()
        {
            KeyValuePair<int, float> result = innerList[0];
            int num = 0;
            int count = innerList.Count;
            innerList[0] = innerList[count - 1];
            innerList.RemoveAt(count - 1);
            count = innerList.Count;
            while (true)
            {
                int num2 = num;
                int num3 = 2 * num + 1;
                int num4 = num3 + 1;
                if (num3 < count && CompareElements(num, num3) > 0)
                {
                    num = num3;
                }

                if (num4 < count && CompareElements(num, num4) > 0)
                {
                    num = num4;
                }

                if (num == num2)
                {
                    break;
                }

                SwapElements(num, num2);
            }

            return result;
        }

        public void Clear()
        {
            innerList.Clear();
        }

        protected void SwapElements(int i, int j)
        {
            KeyValuePair<int, float> value = innerList[i];
            innerList[i] = innerList[j];
            innerList[j] = value;
        }

        protected int CompareElements(int i, int j)
        {
            return comparer.Compare(innerList[i], innerList[j]);
        }
    }
}
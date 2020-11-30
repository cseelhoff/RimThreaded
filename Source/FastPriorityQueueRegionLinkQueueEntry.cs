using System.Collections.Generic;
using static RimThreaded.RegionCostCalculator_Patch;

namespace RimThreaded
{
    public class FastPriorityQueueRegionLinkQueueEntry2
    {
        protected List<RegionLinkQueueEntry2> innerList = new List<RegionLinkQueueEntry2>();

        protected IComparer<RegionLinkQueueEntry2> comparer;

        public int Count => innerList.Count;

        public FastPriorityQueueRegionLinkQueueEntry2()
        {
            comparer = Comparer<RegionLinkQueueEntry2>.Default;
        }

        public FastPriorityQueueRegionLinkQueueEntry2(IComparer<RegionLinkQueueEntry2> comparer)
        {
            this.comparer = comparer;
        }

        public void Push(RegionLinkQueueEntry2 item)
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

        public RegionLinkQueueEntry2 Pop()
        {
            RegionLinkQueueEntry2 result = innerList[0];
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
            RegionLinkQueueEntry2 value = innerList[i];
            innerList[i] = innerList[j];
            innerList[j] = value;
        }

        protected int CompareElements(int i, int j)
        {
            return comparer.Compare(innerList[i], innerList[j]);
        }
    }
}
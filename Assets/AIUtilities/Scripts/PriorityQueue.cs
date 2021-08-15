using System.Collections.Generic;
using System;
namespace fzmnm
{
    //A temporal placeholder for .Net6.x Periority Queue

    public class PriorityQueue<TElement, TProirity> where TProirity : IComparable
    {
        private List<(TElement,TProirity)> data;
        public PriorityQueue() { data = new List<(TElement, TProirity)>(); }
        //Minimal is perioritized
        public void Enqueue(TElement element, TProirity priority)
        {
            int i = data.Count;
            data.Add((element, priority));
            while (i > 0)
            {
                int p = i >> 1;
                if (priority.CompareTo(data[p].Item2) < 0)
                {
                    data[i] = data[p];
                    i = p;
                }
                else break;
            }
            data[i] = (element, priority);
        }
        public void Dequeue(out TElement element, out TProirity priority)
        {
            (element, priority) = data[0];
            if (data.Count > 1)
            {
                int N = data.Count - 1;
                TProirity p = data[N].Item2;
                int i = 0;
                while(true)
                {
                    int c, l = (i << 1)+1,r= l + 1;
                    if (r < N) c = data[l].Item2.CompareTo(data[r].Item2) < 0 ? l : r;
                    else if (l < N) c = l;
                    else break;

                    if (p.CompareTo(data[c].Item2) <= 0) break;
                    else
                    {
                        data[i] = data[c];
                        i = c;
                    }
                }
                data[i] = data[N];
            }
            data.RemoveAt(data.Count-1);
        }
        public int Count => data.Count;
        public void Clear() => data.Clear();
    }
}

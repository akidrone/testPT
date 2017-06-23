using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ALOG.Utilities
{
    public class History<T>
    {
        public History(TimeSpan trackingPeriod)
        {
            this.trackingPeriod = trackingPeriod;
        }

        public void Add(DateTime when, T result)
        {
            CheckIsTimeNewerThanLast(when);
            Clear(when);
            history.Add(new Tuple<DateTime, T>(when, result));
        }

        public int Size
        {
            get
            {
                return history.Count;
            }
        }

        public IEnumerable<Tuple<DateTime, T>> Data
        {
            get
            {
                return history;
            }
        }

        public void Clear()
        {
            history.Clear();
        }

        public void Clear(DateTime dt)
        {
            while(TryRemoveFirst(dt))
            {
            }
        }

        private bool TryRemoveFirst(DateTime dt)
        {
            if (history.Count == 0 || history[0].Item1 + trackingPeriod > dt)
                return false;
            history.RemoveAt(0);
            return true;
        }

        private void CheckIsTimeNewerThanLast(DateTime when)
        {
            var last = history.LastOrDefault();
            if (last != null && when <= last.Item1)
                throw new ArgumentException("Time of new element is not newer than last element");
        }

        private readonly List<Tuple<DateTime, T>> history = new List<Tuple<DateTime, T>>();
        private readonly TimeSpan trackingPeriod;
    }
}

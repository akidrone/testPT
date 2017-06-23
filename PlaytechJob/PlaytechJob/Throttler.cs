using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ALOG.Utilities
{
    public class Throttler
    {
        public Throttler(string throttleConfiguration)
        {
            ConfigureThrottling(throttleConfiguration);
        }

        public void AddToHistory(DateTime when, bool result)
        {
            history.Add(when, result);
        }

        public bool IsThrottled(DateTime dt)
        {
            history.Clear(dt);
            foreach (var t in throttlingConfig)
                if (HistoryHasAtLeast(t.Item1, dt - t.Item2))
                    return true;
            return false;
        }

        public int GetPeriodSeconds()
        {
            return (int)biggestSpan.TotalSeconds;
        }

        private void ConfigureThrottling(string throttleConfiguration)
        {
            if (throttleConfiguration == null)
                return;
            foreach (string str in throttleConfiguration.Split(','))
            {
                string[] a = str.Split('/');
                if (a.Length == 2)
                    throttlingConfig.Add(new Tuple<int, TimeSpan>(int.Parse(a[0]), TimeSpan.FromSeconds(int.Parse(a[1]))));
            }
            foreach (var t in throttlingConfig)
                if (t.Item2 > biggestSpan)
                    biggestSpan = t.Item2;
            history = new History<bool>(biggestSpan);
        }

        private bool HistoryHasAtLeast(int threshold, DateTime newerThan)
        {
            return history.Data.Count(x => x.Item1 > newerThan) >= threshold;
        }

        private History<bool> history;
        private readonly List<Tuple<int, TimeSpan>> throttlingConfig = new List<Tuple<int, TimeSpan>>();
        private TimeSpan biggestSpan = TimeSpan.FromTicks(0);

    }
}
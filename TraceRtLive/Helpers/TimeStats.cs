namespace TraceRtLive.Helpers
{
    public class TimeStats
    {
        public TimeSpan Min 
        {
            get
            {
                lock (_lock) { return _min; }
            }
        }

        public TimeSpan Max
        {
            get
            {
                lock (_lock) { return _max; }
            }
        }
        public TimeSpan Mean
        {
            get
            {
                lock (_lock) { return TimeSpan.FromTicks((long)_meanTicks); }
            }
        }

        private object _lock = new object();

        private TimeSpan _min = TimeSpan.MaxValue;
        private TimeSpan _max = TimeSpan.MinValue;
        private double _meanTicks;
        private long _sumTicks;
        private long _num;

        public void Add(TimeSpan value)
        {
            lock (_lock)
            {
                if (value < _min) _min = value;
                if (value > _max) _max = value;

                _sumTicks += value.Ticks;
                _num += 1;

                _meanTicks = _sumTicks / _num;
            }
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Memory.Etw.Trace.Utils
{
    /// <summary>
    /// Allows time-based throttling the execution of a method/delegate. Only one execution per given time span is performed.
    /// </summary>
    internal class TimeSpanThrottle
    {
        readonly TimeSpan _throttlingTimeSpan;
        readonly object _lockObject = new object();

        DateTimeOffset? _lastExecutionTime;

        public TimeSpanThrottle(TimeSpan throttlingTimeSpan)
        {
            _throttlingTimeSpan = throttlingTimeSpan;
            _lockObject = new object();
        }

        /// <summary>
        /// Only one action can be triggered during a given timespan.
        /// If the timespan is zero or negative, then there is no throttling.
        /// </summary>
        /// <param name="work">The action to be executed</param>
        public void Execute(Action work)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            if (TooEarly(now))
            {
                return;
            }

            lock (_lockObject)
            {
                if (TooEarly(now))
                {
                    return;
                }

                _lastExecutionTime = now;
            }
            work();
        }

        private bool TooEarly(DateTimeOffset now)
        {
            return _throttlingTimeSpan.TotalMilliseconds <= 0
                ? false
                : _lastExecutionTime != null && (now - _lastExecutionTime) < _throttlingTimeSpan;
        }
    }
}

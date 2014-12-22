using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinqToSqlRetry
{
    public class ExponentialRetry : IRetryPolicy
    {
        private static readonly TimeSpan DefaultInitialInterval = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan DefaultIntervalDelta = TimeSpan.FromSeconds(5);
        private const int DefaultRetryCount = 3;

        private readonly TimeSpan _initialInterval;
        private readonly TimeSpan _intervalDelta;
        private readonly int _retryCount;
        private readonly int[] _transientErrors;

        public ExponentialRetry()
            : this(DefaultInitialInterval, DefaultIntervalDelta, DefaultRetryCount)
        {
        }

        public ExponentialRetry(TimeSpan initialInterval, TimeSpan intervalDelta, int retryCount)
            : this(initialInterval, intervalDelta, retryCount, LinearRetry.DefaultTransientErrors)
        {
        }

        public ExponentialRetry(TimeSpan initialInterval, TimeSpan intervalDelta, int retryCount, int[] transientErrors)
        {
            _initialInterval = initialInterval;
            _intervalDelta = intervalDelta;
            _retryCount = retryCount;
            _transientErrors = transientErrors;
        }

        public TimeSpan InitialInterval
        {
            get { return _initialInterval; }
        }

        public TimeSpan IntervalDelta
        {
            get { return _intervalDelta; }
        }

        public int RetryCount
        {
            get { return _retryCount; }
        }

        public int[] TransientErrors
        {
            get { return _transientErrors; }
        }

        public virtual TimeSpan? ShouldRetry(int retryCount, Exception exception)
        {
            SqlException sqlException = exception as SqlException;
            return sqlException != null 
                && _transientErrors.Contains(sqlException.Number) 
                && retryCount < _retryCount 
                ? (TimeSpan?)_initialInterval.Add(TimeSpan.FromMilliseconds(_intervalDelta.TotalMilliseconds * retryCount)) 
                : null;
        }
    }
}

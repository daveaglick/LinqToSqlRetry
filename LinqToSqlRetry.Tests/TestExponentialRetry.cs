using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinqToSqlRetry.Tests
{
    public class TestExponentialRetry : ExponentialRetry
    {
        public override TimeSpan? ShouldRetry(int retryCount, Exception exception)
        {
            TestSqlException sqlException = exception as TestSqlException;
            return sqlException != null
                && retryCount < RetryCount
                ? (TimeSpan?)InitialInterval.Add(TimeSpan.FromMilliseconds(IntervalDelta.TotalMilliseconds * retryCount))
                : null;
        }
    }
}

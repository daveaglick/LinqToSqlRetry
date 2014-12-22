using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinqToSqlRetry.Tests
{
    public class TestLinearRetry : LinearRetry
    {
        public override TimeSpan? ShouldRetry(int retryCount, Exception exception)
        {
            TestSqlException sqlException = exception as TestSqlException;
            return sqlException != null
                && retryCount < RetryCount
                ? (TimeSpan?)Interval
                : null;
        }
    }
}

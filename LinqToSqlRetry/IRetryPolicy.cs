using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinqToSqlRetry
{
    public interface IRetryPolicy
    {
        // Return a TimeSpan indicating the interval to wait until the next retry or null to rethrow the Exception
        TimeSpan? ShouldRetry(int retryCount, Exception exception);
    }
}

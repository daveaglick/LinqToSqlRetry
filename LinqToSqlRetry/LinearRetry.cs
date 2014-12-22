using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinqToSqlRetry
{
    public class LinearRetry : IRetryPolicy
    {
        private static readonly TimeSpan DefaultInterval = TimeSpan.FromSeconds(10);
        private const int DefaultRetryCount = 3;

        // From http://social.technet.microsoft.com/wiki/contents/articles/1541.windows-azure-sql-database-connection-management.aspx with some extras
        internal static readonly int[] DefaultTransientErrors =
        {
            -2,
            // Timeout expired. The timeout period elapsed prior to completion of the operation or the server is not responding.
            20, // The instance of SQL Server you attempted to connect to does not support encryption.
            64,
            // A connection was successfully established with the server, but then an error occurred during the login process. 
            233,
            // The client was unable to establish a connection because of an error during connection initialization process before login.
            10053, // A transport-level error has occurred when receiving results from the server
            10054, // A transport-level error has occurred when sending the request to the server.
            10060,
            // A network-related or instance-specific error occurred while establishing a connection to SQL Server.
            40143, // The service has encountered an error processing your request. Please try again.
            40197, // The service has encountered an error processing your request. Please try again.
            40501, // The service is currently busy. Retry the request after 10 seconds.
            40544, // The database has reached its size quota.
            40549, // Session is terminated because you have a long-running transaction.
            40550, // The session has been terminated because it has acquired too many locks.
            40551, // The session has been terminated because of excessive TEMPDB usage.
            40552, // The session has been terminated because of excessive transaction log space usage.
            40553, // The session has been terminated because of excessive memory usage.
            40613 // Database ... is not currently available.
        };

        private readonly TimeSpan _interval;
        private readonly int _retryCount;
        private readonly int[] _transientErrors;

        public LinearRetry()
            : this(DefaultInterval, DefaultRetryCount)
        {
        }

        public LinearRetry(TimeSpan interval, int retryCount)
            : this(interval, retryCount, DefaultTransientErrors)
        {
        }

        public LinearRetry(TimeSpan interval, int retryCount, int[] transientErrors)
        {
            _interval = interval;
            _retryCount = retryCount;
            _transientErrors = transientErrors;
        }

        public TimeSpan Interval
        {
            get {  return _interval; }
        }

        public int RetryCount
        {
            get {  return _retryCount; }
        }

        public int[] TransientErrors
        {
            get {  return _transientErrors; }
        }

        public virtual TimeSpan? ShouldRetry(int retryCount, Exception exception)
        {
            SqlException sqlException = exception as SqlException;
            return sqlException != null 
                && _transientErrors.Contains(sqlException.Number) 
                && retryCount < _retryCount 
                ? (TimeSpan?)_interval 
                : null;
        }
    }
}

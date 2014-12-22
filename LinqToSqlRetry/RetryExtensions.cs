using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToSqlRetry
{
    public static class RetryExtensions
    {
        public static IQueryable<T> Retry<T>(this IQueryable<T> queryable)
        {
            return Retry(queryable, new LinearRetry());
        }

        public static IQueryable<T> Retry<T>(this IQueryable<T> queryable, IRetryPolicy retryPolicy)
        {
            IQueryProvider provider = new RetryQueryProvider(queryable.Provider, retryPolicy);
            return provider.CreateQuery<T>(queryable.Expression);
        }

        public static void SubmitChangesRetry(this DataContext dataContext)
        {
            SubmitChangesRetry(dataContext, new LinearRetry());
        }

        public static void SubmitChangesRetry(this DataContext dataContext, ConflictMode failureMode)
        {
            SubmitChangesRetry(dataContext, failureMode, new LinearRetry());
        }

        public static void SubmitChangesRetry(this DataContext dataContext, IRetryPolicy retryPolicy)
        {
            retryPolicy.Retry(() => dataContext.SubmitChanges());
        }

        public static void SubmitChangesRetry(this DataContext dataContext, ConflictMode failureMode, IRetryPolicy retryPolicy)
        {
            retryPolicy.Retry(() => dataContext.SubmitChanges(failureMode));
        }

        public static void Retry(this IRetryPolicy retryPolicy, Action action)
        {
            retryPolicy.Retry<object>(() => { action(); return null; });
        }

        public static T Retry<T>(this IRetryPolicy retryPolicy, Func<T> func)
        {
            int retryCount = 0;
            while (true)
            {
                try
                {
                    return func();
                }
                catch (Exception ex)
                {
                    TimeSpan? interval = retryPolicy.ShouldRetry(retryCount, ex);
                    if (!interval.HasValue)
                    {
                        throw;
                    }
                    Thread.Sleep(interval.Value);
                }
                retryCount++;
            }
        }
    }
}

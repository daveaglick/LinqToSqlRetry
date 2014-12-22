using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace LinqToSqlRetry
{
    internal class RetryQueryProvider : IQueryProvider
    {
        private readonly IQueryProvider _queryProvider;
        private readonly IRetryPolicy _retryPolicy;

        public RetryQueryProvider(IQueryProvider queryProvider, IRetryPolicy retryPolicy)
        {
            _queryProvider = queryProvider;
            _retryPolicy = retryPolicy;
        }

        public virtual IQueryable CreateQuery(Expression expression)
        {
            return new RetryQueryable(this, _queryProvider.CreateQuery(expression), _retryPolicy);
        }

        public virtual IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new RetryQueryable<TElement>(this, _queryProvider.CreateQuery<TElement>(expression), _retryPolicy);
        }

        // The Execute method executes queries that return a single value (instead of an enumerable sequence of values). 
        // Expression trees that represent queries that return enumerable results are executed when their associated IQueryable object is enumerated.
        // From http://msdn.microsoft.com/en-us/library/bb535032(v=vs.100).aspx

        public object Execute(Expression expression)
        {
            return _retryPolicy.Retry(() => _queryProvider.Execute(expression));
        }

        public TResult Execute<TResult>(Expression expression)
        {
            return _retryPolicy.Retry(() => _queryProvider.Execute<TResult>(expression));
        }
    }
}

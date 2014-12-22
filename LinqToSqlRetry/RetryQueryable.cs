using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace LinqToSqlRetry
{
    internal class RetryQueryable : IOrderedQueryable
    {
        private readonly IQueryable _queryable;
        private readonly IQueryProvider _queryProvider;
        private readonly IRetryPolicy _retryPolicy;

        public RetryQueryable(IQueryProvider queryProvider, IQueryable queryable, IRetryPolicy retryPolicy)
        {
            _queryable = queryable;
            _queryProvider = queryProvider;
            _retryPolicy = retryPolicy;
        }

        public Expression Expression
        {
            get { return _queryable.Expression; }
        }

        public Type ElementType
        {
            get { return _queryable.ElementType; }
        }

        public IQueryProvider Provider
        {
            get { return _queryProvider; }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _retryPolicy.Retry(() => _queryable.GetEnumerator());
        }

        public override string ToString()
        {
            return _queryable.ToString();
        }

        protected IRetryPolicy RetryPolicy
        {
            get { return _retryPolicy; }
        }
    }

    internal class RetryQueryable<T> : RetryQueryable, IOrderedQueryable<T>
    {
        private readonly IQueryable<T> _queryable;

        public RetryQueryable(IQueryProvider queryProvider, IQueryable<T> queryable, IRetryPolicy retryPolicy)
            : base(queryProvider, queryable, retryPolicy)
        {
            _queryable = queryable;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return RetryPolicy.Retry(() => _queryable.GetEnumerator());
        }
    }
}

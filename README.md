LinqToSqlRetry
==============

Available on NuGet: https://www.nuget.org/packages/LinqToSqlRetry/

A simple library to help manage retries in LINQ to SQL. This is particularly important in cloud-based infrastructures like Azure where transient failures are not uncommon. And despite the popularity of Entity Framework, Dapper, and other ORM or data access libraries, there is still a place for simple LINQ to SQL code.

Retry logic is provided via extension methods, so you will need to bring the `LinqToSqlRetry` namespace into scope in every file you need retry logic:
```
using LinqtoSqlRetry;
```

## Retry On Submit Changes

Instead of using `DataContext.SubmitChanges()` just use `DataContext.SubmitChangesRetry()`:
```
using(var context = new MyDbContext())
{
  context.Items.InsertOnSubmit(new Item { Name = "ABC" });
  context.SubmitChangesRetry();
}
```

## Retry on Queries

Add the `Retry()` extension method to the end of your queries. It should generally be the last extension you use before materializing the query (which happens when you use extensions like `ToList()` or `Count()`):

```
using(var context = new MyDbContext())
{
  int count = context.Items.Where(x => x.Name == "ABC").Retry().Count();
}
```

## Retry Policy

The retry logic is controlled by a policy that indicates when a retry should take place and how long to wait before retrying the operation. Two policies are supplied:

* `LinearRetry` retries a specific number of times (3 by default) and waits a specified amount each time (10 seconds by default).
* `ExponentialRetry` retries a specific number of times (3 by default) and waits an increasing multiple of time (5 seconds by default) after an initial wait on the first retry (10 seconds by default).

By default the `LinearRetry` policy is used. The policy that you use and it's settings can be changed by passing it in to any of the extension methods:

```
using(var context = new MyDbContext())
{
  // Start with a 4 second wait, increasing by a factor of 2, for 5 attempts
  var retryPolicy = new ExponentialRetry(TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(2), 5);
  context.Items.InsertOnSubmit(new Item { Name = "ABC" });
  context.SubmitChangesRetry(retryPolicy);
  int count = context.Items.Where(x => x.Name == "ABC").Retry(retryPolicy).Count();
}
```

You can specify your own custom policies to do things like log retry attempts, use more complex logic, retry on different types of errors, etc. by implementing `IRetryPolicy`.

## Retry for Arbitrary Operations

You can also retry any arbitrary operation with the `Retry()` extension methods on any `IRetryPolicy` object. There are two of them, one takes an `Action` and the other takes a `Func<T>` and returns the result. For example:

```
var retryPolicy = new ExponentialRetry(TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(2), 5);
retryPolicy.Retry(() => AMethodThatMightFail());
```

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace LinqToSqlRetry.Tests
{
    [TestFixture]
    public class RetryExtensionsFixture
    {
        [Test]
        public void QueryableEnumerateThrowsException()
        {
            using (var context = new MemoryTestDataContext())
            {
                InitializeData(context);
                context.RetryCount = 1;
                Assert.Throws<TestSqlException>(() => context.TestObjects.Where(x => x.Bool).ToList().Count());
            }
        }

        [Test]
        public void QueryableEnumerateSucceedsOnRetry()
        {
            using (var context = new MemoryTestDataContext())
            {
                InitializeData(context);
                context.RetryCount = 1;
                Assert.AreEqual(2, context.TestObjects.Where(x => x.Bool).Retry(new TestLinearRetry()).ToList().Count());
            }
        }

        [Test]
        public void QueryableExecuteThrowsException()
        {
            using (var context = new MemoryTestDataContext())
            {
                InitializeData(context);
                context.RetryCount = 1;
                Assert.Throws<TestSqlException>(() => context.TestObjects.Where(x => x.Bool).Count());
            }
        }

        [Test]
        public void QueryableExecuteSucceedsOnRetry()
        {
            using (var context = new MemoryTestDataContext())
            {
                InitializeData(context);
                context.RetryCount = 1;
                Assert.AreEqual(2, context.TestObjects.Where(x => x.Bool).Retry(new TestLinearRetry()).Count());
            }
        }

        [Test]
        public void SubmitChangesThrowsException()
        {
            using (var context = new MemoryTestDataContext())
            {
                InitializeData(context);
                context.RetryCount = 1;
                context.TestObjects.InsertOnSubmit(new TestObject());
                Assert.Throws<TestSqlException>(() => context.SubmitChanges());
            }
        }

        [Test]
        public void SubmitChangesSucceedsOnRetry()
        {
            using (var context = new MemoryTestDataContext())
            {
                InitializeData(context);
                context.RetryCount = 1;
                context.TestObjects.InsertOnSubmit(new TestObject());
                Assert.DoesNotThrow(() => new TestLinearRetry().Retry(() => context.SubmitChanges()));
                Assert.AreEqual(4, context.TestObjects.Count());
            }
        }

        private void InitializeData(MemoryTestDataContext context)
        {
            context.TestObjects.InsertOnSubmit(new TestObject
            {
                String = "A",
                Number = 10,
                Bool = true
            });
            context.TestObjects.InsertOnSubmit(new TestObject
            {
                String = "B",
                Number = 100,
                Bool = true
            });
            context.TestObjects.InsertOnSubmit(new TestObject
            {
                String = "C",
                Number = 1000,
                Bool = false
            });
            context.SubmitChanges();
        }
    }
}

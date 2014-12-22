// From https://github.com/lukesampson/LinqToSQL-test-extensions/ with modifications

// 
// Auto-generated from E:\Code\LinqToSqlRetry\LinqToSqlRetry.Tests\Test.dbml
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.ComponentModel;

namespace LinqToSqlRetry.Tests {

	#region ITestDataContext
	public interface ITestDataContext : IDisposable {
				
		ITable<TestObject> TestObjects { get; }
		
		ITable<TEntity> GetTable<TEntity>() where TEntity: class;

		void SubmitChanges();
	}
	#endregion

	#region Partial class extensions to TestDataContext
	public partial class TestDataContext : ITestDataContext {
				
		ITable<TestObject> ITestDataContext.TestObjects {
			get { return this.TestObjects; }
		}
		
		ITable<TEntity> ITestDataContext.GetTable<TEntity>() {
			return this.GetTable<TEntity>();
		}
		
	}
	#endregion
	
	#region In-memory version of ITestDataContext
	public class MemoryTestDataContext : IMockDataContext, ITestDataContext {

		Dictionary<Type, ITable> _tables = new Dictionary<Type, ITable>();
		static MappingSource mappingSource = new AttributeMappingSource();

				
		public ITable<TestObject> TestObjects {
			get { return GetTable<TestObject>(); }
		}
		
		public ITable GetTable(Type type) {
			ITable table;
			if(!_tables.TryGetValue(type, out table)) {
				var tableType = typeof(MemoryTable<>).MakeGenericType(type);

				table = Activator.CreateInstance(tableType, this) as ITable;
				_tables.Add(type, table);
			}
			return table;
		}

		public ITable<TEntity> GetTable<TEntity>() where TEntity : class {
			var type = typeof(TEntity);

			ITable table;
			if(!_tables.TryGetValue(type, out table)) {
				table = new MemoryTable<TEntity>(this);
				_tables.Add(type, table);
			}

			return (ITable<TEntity>)table;
		}


		public void SubmitChanges() {
			foreach(var table in _tables.Values) {
				((IMockTable)table).SubmitChanges();
			}
		}

		public MetaModel Mapping {
			get {
				return mappingSource.GetModel(typeof(ITestDataContext));
			}
		}

		public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
		}

        protected virtual void Dispose(bool disposing) {
        }

		public int? RetryCount { get; set; }

		public void CheckThrow()
		{
			if(RetryCount.HasValue && RetryCount.Value > 0)
			{
				RetryCount = (int?)RetryCount.Value - 1;
				throw new TestSqlException();
			}
		}
	}
	#endregion
	
	#region Supporting code for in-memory testing
	public interface IMockDataContext {
		MetaModel Mapping { get; }
		ITable<TEntity> GetTable<TEntity>() where TEntity: class;
		ITable GetTable(Type type);
		void CheckThrow();
	}
	
	public interface IMockTable {
		void SubmitChanges();
	}
	
	#region MemoryTable
	public class MemoryTable<TEntity> : IMockTable, ITable, ITable<TEntity> where TEntity : class {
		IMockDataContext _context;
		MetaTable _metaTable;

		List<TEntity> _stored = new List<TEntity>();
		List<TEntity> _insert = new List<TEntity>();
		List<TEntity> _delete = new List<TEntity>();

		int _nextId = 1;

		object lockObj = new object();

		public MemoryTable(IMockDataContext context) {
			_context = context;
			_metaTable = _context.Mapping.GetTable(typeof(TEntity));
		}

		public void SubmitChanges() {
			_context.CheckThrow();
			var ids = _metaTable.RowType.IdentityMembers;
			var autoIds = ids.Where(id => id.IsDbGenerated);
			if(autoIds.Count() > 1) throw new NotImplementedException("Can't handle more than one auto-generated identity column!");

			lock(lockObj) {
				foreach(var insert in _insert) {
					if(autoIds.Count() > 0) {
						((PropertyInfo)autoIds.First().Member).SetValue(insert, _nextId++, null);
					}

					_stored.Add(insert);
				}

				foreach(var entity in _delete) {
					_stored.Remove(entity);
				}

				_insert.Clear();
			}
		}

		public void Attach(TEntity entity) {
			throw new NotImplementedException();
		}

		public void DeleteOnSubmit(TEntity entity) {
			_delete.Add(entity);
		}

		public void InsertOnSubmit(TEntity entity) {
			lock(lockObj) {
				var rowType = _metaTable.RowType;

				// insert linked objects
				var fk_assocs = rowType.Associations.Where(a => a.IsForeignKey);
				foreach(var fk_assoc in fk_assocs) {
					var prop = (PropertyInfo)fk_assoc.ThisMember.Member;
					var fk_entity = prop.GetValue(entity, null);
					if(fk_entity != null) {
						if(fk_assoc.OtherKey.Count > 1) continue; // don't know how to handle more than one key!
						var key = fk_assoc.OtherKey[0].MappedName;

						// update the FK property
						// not easy to do, because the entity reference is already set
						if(fk_assoc.OtherMember == null) {
							// no child property
							// have to get a bit evil and use reflection on private field in this case
							((INotifyPropertyChanged)fk_entity).PropertyChanged += (sender, e) => {
								if(e.PropertyName == key) {
									var thisKeyName = fk_assoc.ThisKey[0].Name;
									var idField = entity.GetType().GetField("_" + thisKeyName, BindingFlags.Instance | BindingFlags.NonPublic);

									var otherKey = fk_entity.GetType().GetProperty(key);
									var otherKeyVal = otherKey.GetValue(fk_entity, null);

									idField.SetValue(entity, otherKeyVal);
								}
							};
						} else {
							((INotifyPropertyChanged)fk_entity).PropertyChanged += (sender, e) => {
								if(e.PropertyName == key) {
									prop.SetValue(entity, null, null);
									prop.SetValue(entity, fk_entity, null);
								}
							};
						}
						_context.GetTable(fk_entity.GetType()).InsertOnSubmit(fk_entity);
					}
				}

				_insert.Add(entity);
			}
		}

		public IEnumerator<TEntity> GetEnumerator() {
			return _stored.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
			return _stored.GetEnumerator();
		}

		public Type ElementType {
			get { return typeof(TEntity); }
		}

		public System.Linq.Expressions.Expression Expression {
			get {
				return _stored.AsQueryable().Expression;
			}
		}

		public IQueryProvider Provider {
			get {
				return new CaseInsensitiveQueries(_stored.AsQueryable().Provider, _context);
			}
		}

		#region ITable members
		public void Attach(object entity, object original) {
			throw new NotImplementedException();
		}

		public void Attach(object entity, bool asModified) {
			throw new NotImplementedException();
		}

		public void Attach(object entity) {
			throw new NotImplementedException();
		}

		public void AttachAll(System.Collections.IEnumerable entities, bool asModified) {
			throw new NotImplementedException();
		}

		public void AttachAll(System.Collections.IEnumerable entities) {
			throw new NotImplementedException();
		}

		public DataContext Context {
			get { throw new NotSupportedException(); }
		}

		public void DeleteAllOnSubmit(System.Collections.IEnumerable entities) {
			throw new NotImplementedException();
		}

		public void DeleteOnSubmit(object entity) {
			DeleteOnSubmit((TEntity)entity);
		}

		public ModifiedMemberInfo[] GetModifiedMembers(object entity) {
			throw new NotImplementedException();
		}

		public object GetOriginalEntityState(object entity) {
			throw new NotImplementedException();
		}

		public void InsertAllOnSubmit(System.Collections.IEnumerable entities) {
			foreach(var entity in entities) {
				InsertOnSubmit(entity);
			}
		}

		public void InsertOnSubmit(object entity) {
			InsertOnSubmit((TEntity)entity);
		}

		public bool IsReadOnly {
			get { return false; }
		}
		#endregion
	}
	#endregion

	#region CaseInsensitiveQueries
	/// <summary>
	/// Modifies expressions to mimic SQL Server's case-insenstive string comparison
	/// </summary>
	public class CaseInsensitiveQueries : ExpressionVisitor, IQueryProvider {
		public static bool Equal(string a, string b) {
			return a.Equals(b, StringComparison.OrdinalIgnoreCase);
		}

		public static bool NotEqual(string a, string b) {
			return !a.Equals(b, StringComparison.OrdinalIgnoreCase);
		}

		public static bool Contains(string a, string b) {
			return a.IndexOf(b, StringComparison.OrdinalIgnoreCase) != -1;
		}

		IMockDataContext _context;
		IQueryProvider _provider;

		public CaseInsensitiveQueries(IQueryProvider provider, IMockDataContext context) {
			_provider = provider;
			_context = context;
		}

		protected override Expression VisitBinary(BinaryExpression node) {
			if(node.Method != null && node.Method.DeclaringType == typeof(string)) {
				if(node.NodeType == ExpressionType.Equal) {
					var method = this.GetType().GetMethod("Equal");
					return Expression.MakeBinary(ExpressionType.Equal, node.Left, node.Right, node.IsLiftedToNull, method);
				} else if(node.NodeType == ExpressionType.NotEqual) {
					var method = this.GetType().GetMethod("NotEqual");
					return Expression.MakeBinary(ExpressionType.Equal, node.Left, node.Right, node.IsLiftedToNull, method);
				}
			}

			return base.VisitBinary(node);
		}

		protected override Expression VisitMethodCall(MethodCallExpression node) {
			if(node.Method.DeclaringType == typeof(string)) {
				if(node.Method.Name == "Contains") {
					var method = this.GetType().GetMethod("Contains");
					return Expression.Call(method, node.Object, node.Arguments[0]);
				}
			}
			return base.VisitMethodCall(node);
		}

		public IQueryable<TElement> CreateQuery<TElement>(Expression expression) {
			return new TestQueryable<TElement>(this, _provider.CreateQuery<TElement>(Visit(expression)), _context);
		}

		public IQueryable CreateQuery(Expression expression) {
			return new TestQueryable(this, _provider.CreateQuery(Visit(expression)), _context);
		}
		
		public TResult Execute<TResult>(Expression expression) {
			_context.CheckThrow();
			return _provider.Execute<TResult>(Visit(expression));
		}

		public object Execute(Expression expression) {
			_context.CheckThrow();
			return _provider.Execute(Visit(expression));
		}
	}

	public class TestQueryable : IOrderedQueryable
    {
        private readonly IQueryable _queryable;
        private readonly IQueryProvider _queryProvider;

		protected IMockDataContext Context { get; private set; }

        public TestQueryable(IQueryProvider queryProvider, IQueryable queryable, IMockDataContext context)
        {
            _queryable = queryable;
            _queryProvider = queryProvider;
			Context = context;
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
			Context.CheckThrow();
            return _queryable.GetEnumerator();
        }

        public override string ToString()
        {
            return _queryable.ToString();
        }
    }

    internal class TestQueryable<T> : TestQueryable, IOrderedQueryable<T>
    {
        private readonly IQueryable<T> _queryable;

        public TestQueryable(IQueryProvider queryProvider, IQueryable<T> queryable, IMockDataContext context)
            : base(queryProvider, queryable, context)
        {
            _queryable = queryable;
        }

        public IEnumerator<T> GetEnumerator()
        {
			Context.CheckThrow();
            return _queryable.GetEnumerator();
        }
    }

	#endregion
	
	public static class ITableExtensions {
		public static void InsertAllOnSubmit<TEntity>(this ITable<TEntity> table, IEnumerable<TEntity> entities) where TEntity : class {
			var realTable = table as Table<TEntity>;
			if(realTable != null) {
				realTable.InsertAllOnSubmit(entities);
				return;
			}
			
			foreach(var entity in entities) {
				table.InsertOnSubmit(entity);
			}
		}

		public static void DeleteAllOnSubmit<TEntity>(this ITable<TEntity> table, IEnumerable<TEntity> entities) where TEntity : class {
			var realTable = table as Table<TEntity>;
			if(realTable != null) {
				realTable.DeleteAllOnSubmit(entities);
				return;
			}
			
			foreach(var entity in entities) {
				table.DeleteOnSubmit(entity);
			}
		}
	}
	
	#endregion
}
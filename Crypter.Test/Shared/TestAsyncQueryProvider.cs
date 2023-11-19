using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Query;

namespace Crypter.Test.Shared
{
   /// <summary>
   /// Represents a test async query provider.
   /// </summary>
   /// <typeparam name="TEntity">The type of the entities in the query.</typeparam>
   internal class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
   {
      private readonly IQueryProvider _inner;

      /// <summary>
      /// Initializes a new instance of the <see cref="TestAsyncQueryProvider{TEntity}"/> class.
      /// </summary>
      /// <param name="inner">The inner query provider.</param>
      internal TestAsyncQueryProvider(IQueryProvider inner) => _inner = inner;

      /// <inheritdoc/>
      public IQueryable CreateQuery(Expression expression) =>
          new TestAsyncEnumerable<TEntity>(expression);

      /// <inheritdoc/>
      public IQueryable<TElement> CreateQuery<TElement>(Expression expression) =>
          new TestAsyncEnumerable<TElement>(expression);

      /// <inheritdoc/>
      public object Execute(Expression expression) => _inner.Execute(expression);

      /// <inheritdoc/>
      public TResult Execute<TResult>(Expression expression) => _inner.Execute<TResult>(expression);

      /// <inheritdoc/>
      public IAsyncEnumerable<TResult> ExecuteAsync<TResult>(Expression expression) =>
          new TestAsyncEnumerable<TResult>(expression);

      /// <inheritdoc/>
      public Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken) =>
          Task.FromResult(Execute<TResult>(expression));

      /// <inheritdoc/>
      TResult IAsyncQueryProvider.ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken) =>
          ExecuteAsync<TResult>(expression, cancellationToken).GetAwaiter().GetResult();
   }
}

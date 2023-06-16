/*
 * Copyright (C) 2023 Crypter File Transfer
 * 
 * This file is part of the Crypter file transfer project.
 * 
 * Crypter is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * The Crypter source code is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 * 
 * You should have received a copy of the GNU Affero General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 * 
 * You can be released from the requirements of the aforementioned license
 * by purchasing a commercial license. Buying such a license is mandatory
 * as soon as you develop commercial activities involving the Crypter source
 * code without disclosing the source code of your own applications.
 * 
 * Contact the current copyright holder to discuss commercial license options.
 */

using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;

namespace Crypter.Test.Shared
{
   /// <summary>
   /// Represents a test async enumerable.
   /// </summary>
   /// <typeparam name="T">The type of the elements in the enumerable.</typeparam>
   internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
   {
      /// <summary>
      /// Initializes a new instance of the <see cref="TestAsyncEnumerable{T}"/> class.
      /// </summary>
      /// <param name="enumerable">The enumerable to use.</param>
      public TestAsyncEnumerable(IEnumerable<T> enumerable)
          : base(enumerable)
      { }

      /// <summary>
      /// Initializes a new instance of the <see cref="TestAsyncEnumerable{T}"/> class.
      /// </summary>
      /// <param name="expression">The expression representing the enumerable.</param>
      public TestAsyncEnumerable(Expression expression)
          : base(expression)
      { }

      /// <inheritdoc/>
      public IAsyncEnumerator<T> GetEnumerator() =>
          new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());

      /// <inheritdoc/>
      public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
          new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());

      /// <inheritdoc/>
      IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
   }
}
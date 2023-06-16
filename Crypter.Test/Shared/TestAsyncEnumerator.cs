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
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.Test.Shared
{
   /// <summary>
   /// This class is used to mock an asynchronous enumerator.
   /// </summary>
   /// <typeparam name="T">Specifies the type of data the enumerator is working with.</typeparam>
   internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
   {
      private readonly IEnumerator<T> _inner;

      /// <summary>
      /// Initializes a new instance of the <see cref="TestAsyncEnumerator{T}"/> class.
      /// </summary>
      /// <param name="inner">The inner enumerator.</param>
      public TestAsyncEnumerator(IEnumerator<T> inner) =>
          _inner = inner;

      /// <inheritdoc/>
      public void Dispose() =>
          _inner.Dispose();

      /// <inheritdoc/>
      public T Current => _inner.Current;

      /// <inheritdoc/>
      public Task<bool> MoveNext(CancellationToken cancellationToken) =>
          Task.FromResult(_inner.MoveNext());

      /// <inheritdoc/>
      public async ValueTask<bool> MoveNextAsync()
      {
         return await Task.FromResult(_inner.MoveNext());
      }

      /// <inheritdoc/>
      public ValueTask DisposeAsync()
      {
         _inner.Dispose();
         return default;
      }
   }

}
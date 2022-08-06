/*
 * Copyright (C) 2022 Crypter File Transfer
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

using Crypter.Common.Monads;
using NUnit.Framework;
using System.Threading.Tasks;

namespace Crypter.Test.Common_Tests.Monad_Tests
{
   [TestFixture]
   [Parallelizable]
   internal class MaybeAsyncExtensions_Tests
   {
      [Test]
      public async Task BindAsync_Matches_None()
      {
         Task<Maybe<int>> isNone = Maybe<int>.None.AsTask();
         Maybe<string> unwrapped = await isNone.BindAsync(x => x.ToString());

         Assert.IsTrue(unwrapped.IsNone);
         unwrapped.IfSome(_ => Assert.Fail());
      }

      [Test]
      public async Task BindAsync_Matches_Some()
      {
         Task<Maybe<int>> isSome = Maybe<int>.From(5).AsTask();
         Maybe<string> unwrapped = await isSome.BindAsync(x => x.ToString());

         Assert.IsTrue(unwrapped.IsSome);
         unwrapped.IfSome(x => Assert.AreEqual("5", x));
         unwrapped.IfNone(() => Assert.Fail());
      }
   }
}

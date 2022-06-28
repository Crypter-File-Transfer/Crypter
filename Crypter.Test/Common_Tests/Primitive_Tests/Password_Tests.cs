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

using Crypter.Common.Primitives;
using Crypter.Common.Primitives.Exceptions;
using NUnit.Framework;

namespace Crypter.Test.Common_Tests.Primitive_Tests
{
   [TestFixture]
   [Parallelizable]
   public class Password_Tests
   {
      [TestCase(null)]
      public void Null_Passwords_Are_Invalid(string password)
      {
         Assert.Throws<ValueNullException>(() => Password.From(password));

         bool shouldBeFalse = Password.TryFrom(password, out var shouldBeNull);
         Assert.IsFalse(shouldBeFalse);
         Assert.IsNull(shouldBeNull);
      }

      [TestCase("")]
      [TestCase(" ")]
      [TestCase("   ")]
      public void Empty_Passwords_Are_Invalid(string password)
      {
         Assert.Throws<ValueEmptyException>(() => Password.From(password));

         bool shouldBeFalse = Password.TryFrom(password, out var shouldBeNull);
         Assert.IsFalse(shouldBeFalse);
         Assert.IsNull(shouldBeNull);
      }

      [TestCase("a")]
      [TestCase(" whitespace ")]
      [TestCase("12345")]
      [TestCase("!@#$%^&")]
      public void Valid_Passwords_Are_Valid(string password)
      {
         Assert.DoesNotThrow(() => Password.From(password));
         var validPassword = Password.From(password);
         Assert.IsNotNull(validPassword);
         Assert.AreEqual(password, validPassword.Value);

         bool shouldBeTrue = Password.TryFrom(password, out var newValidPassword);
         Assert.IsTrue(shouldBeTrue);
         Assert.IsNotNull(newValidPassword);
         Assert.AreEqual(password, newValidPassword.Value);
      }
   }
}

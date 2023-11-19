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

using Crypter.Common.Primitives;
using Crypter.Common.Primitives.Exceptions;
using NUnit.Framework;

namespace Crypter.Test.Common_Tests.Primitive_Tests;

[TestFixture]
[Parallelizable]
public class Username_Tests
{
   [TestCase(null)]
   public void Null_Usernames_Are_Invalid(string username)
   {
      Assert.Throws<ValueNullException>(() => Username.From(username));

      bool shouldBeFalse = Username.TryFrom(username, out var shouldBeNull);
      Assert.IsFalse(shouldBeFalse);
      Assert.IsNull(shouldBeNull);
   }

   [TestCase("")]
   [TestCase(" ")]
   [TestCase("   ")]
   public void Empty_Usernames_Are_Invalid(string username)
   {
      Assert.Throws<ValueEmptyException>(() => Username.From(username));

      bool shouldBeFalse = Username.TryFrom(username, out var shouldBeNull);
      Assert.IsFalse(shouldBeFalse);
      Assert.IsNull(shouldBeNull);
   }

   [TestCase("`")]
   [TestCase("~")]
   [TestCase("!")]
   [TestCase("@")]
   [TestCase("#")]
   [TestCase("$")]
   [TestCase("%")]
   [TestCase("^")]
   [TestCase("&")]
   [TestCase("*")]
   [TestCase("(")]
   [TestCase(")")]
   [TestCase("=")]
   [TestCase("+")]
   [TestCase("\\")]
   [TestCase("|")]
   [TestCase("[")]
   [TestCase("]")]
   [TestCase("{")]
   [TestCase("}")]
   [TestCase(";")]
   [TestCase(":")]
   [TestCase("'")]
   [TestCase("\"")]
   [TestCase(",")]
   [TestCase("<")]
   [TestCase(".")]
   [TestCase(">")]
   [TestCase("/")]
   [TestCase("?")]
   [TestCase("Inv@lid")]
   public void Usernames_With_Invalid_Characters_Are_Invalid(string username)
   {
      Assert.Throws<ValueContainsInvalidCharactersException>(() => Username.From(username));

      bool shouldBeFalse = Username.TryFrom(username, out var shouldBeNull);
      Assert.IsFalse(shouldBeFalse);
      Assert.IsNull(shouldBeNull);
   }

   [TestCase("ThisIsExactly33CharactersInLength")]
   public void Usernames_Longer_Than_32_Characters_Are_Invalid(string username)
   {
      Assert.Throws<ValueTooLongException>(() => Username.From(username));

      bool shouldBeFalse = Username.TryFrom(username, out var shouldBeNull);
      Assert.IsFalse(shouldBeFalse);
      Assert.IsNull(shouldBeNull);
   }

   [TestCase("jack")]
   [TestCase("JACK")]
   [TestCase("1234567890")]
   [TestCase("_-_-_-_")]
   [TestCase("ThisIsPrecisely32Characters_Long")]
   public void Valid_Usernames_Are_Valid(string username)
   {
      Assert.DoesNotThrow(() => Username.From(username));
      var validUsername = Username.From(username);
      Assert.IsNotNull(validUsername);
      Assert.AreEqual(username, validUsername.Value);

      bool shouldBeTrue = Username.TryFrom(username, out var newValidUsername);
      Assert.IsTrue(shouldBeTrue);
      Assert.IsNotNull(newValidUsername);
      Assert.AreEqual(username, newValidUsername.Value);
   }
}
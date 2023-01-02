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

namespace Crypter.Test.Common_Tests.Primitive_Tests
{
   [TestFixture]
   [Parallelizable]
   public class Base64String_Tests
   {
      [TestCase(null)]
      public void Null_Value_Is_Invalid(string value)
      {
         Assert.Throws<ValueNullException>(() => Base64String.From(value));

         bool shouldBeFalse = Base64String.TryFrom(value, out var shouldBeNull);
         Assert.IsFalse(shouldBeFalse);
         Assert.IsNull(shouldBeNull);
      }

      [TestCase("")]
      [TestCase(" ")]
      [TestCase("   ")]
      public void Empty_Values_Are_Invalid(string value)
      {
         Assert.Throws<ValueEmptyException>(() => Base64String.From(value));

         bool shouldBeFalse = Base64String.TryFrom(value, out var shouldBeNull);
         Assert.IsFalse(shouldBeFalse);
         Assert.IsNull(shouldBeNull);
      }

      [TestCase("jellyfish")]
      [TestCase("jellyfish=")]
      [TestCase("jellyfish==")]
      public void Invalid_Base64_Strings_Are_Invalid(string value)
      {
         Assert.Throws<ValueInvalidException>(() => Base64String.From(value));

         bool shouldBeFalse = Base64String.TryFrom(value, out var shouldBeNull);
         Assert.IsFalse(shouldBeFalse);
         Assert.IsNull(shouldBeNull);
      }

      [TestCase("amVsbHlmaXNo")]
      [TestCase("dHVuZHJhIHRvYWQ=")]
      [TestCase("Y3J5cHRlcg==")]
      public void Valid_Base64_Strings_Are_Valid(string value)
      {
         Assert.DoesNotThrow(() => Base64String.From(value));
         var validBase64 = Base64String.From(value);
         Assert.IsNotNull(validBase64);
         Assert.AreEqual(value, validBase64.Value);

         bool shouldBeTrue = Base64String.TryFrom(value, out var newValidBase64);
         Assert.IsTrue(shouldBeTrue);
         Assert.IsNotNull(newValidBase64);
         Assert.AreEqual(value, newValidBase64.Value);
      }
   }
}

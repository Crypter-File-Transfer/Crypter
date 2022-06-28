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
   public class PEMString_Tests
   {
      [SetUp]
      public void Setup()
      {
      }

      [TestCase(null)]
      public void Null_Strings_Are_Invalid(string pemString)
      {
         Assert.Throws<ValueNullException>(() => PEMString.From(pemString));

         bool shouldBeFalse = PEMString.TryFrom(pemString, out var shouldBeNull);
         Assert.IsFalse(shouldBeFalse);
         Assert.IsNull(shouldBeNull);
      }

      [TestCase("")]
      [TestCase(" ")]
      [TestCase("   ")]
      public void Empty_Strings_Are_Invalid(string pemString)
      {
         Assert.Throws<ValueEmptyException>(() => PEMString.From(pemString));

         bool shouldBeFalse = PEMString.TryFrom(pemString, out var shouldBeNull);
         Assert.IsFalse(shouldBeFalse);
         Assert.IsNull(shouldBeNull);
      }

      [TestCase("hello")]
      [TestCase("----foo----")]
      [TestCase("this\nis\nfun")]
      public void Random_Strings_Are_Invalid(string pemString)
      {
         Assert.Throws<ValueInvalidException>(() => PEMString.From(pemString));

         bool shouldBeFalse = PEMString.TryFrom(pemString, out var shouldBeNull);
         Assert.IsFalse(shouldBeFalse);
         Assert.IsNull(shouldBeNull);
      }

      [Test]
      public void Generic_Private_Keys_Are_Valid()
      {
         string privateKey = @"-----BEGIN PRIVATE KEY-----
MFECAQEwBQYDK2VuBCIEIMgIsnIoed7vL0TR4K7XLj/XLKMi3e+Sb5ZQRjxA5Lpg
gSEAj5qsk+931xpwHXrN40pnxXSEz08Hxuhw2wABl+GG9yA=
-----END PRIVATE KEY-----
".ReplaceLineEndings();

         Assert.DoesNotThrow(() => PEMString.From(privateKey));
         PEMString validPemString = PEMString.From(privateKey);
         Assert.IsNotNull(validPemString);
         Assert.AreEqual(privateKey, validPemString.Value);

         bool shouldBeTrue = PEMString.TryFrom(privateKey, out var newValidPemString);
         Assert.IsTrue(shouldBeTrue);
         Assert.IsNotNull(newValidPemString);
         Assert.AreEqual(privateKey, newValidPemString.Value);
      }

      [Test]
      public void Generic_Public_Keys_Are_Valid()
      {
         string publicKey = @"-----BEGIN PUBLIC KEY-----
MCowBQYDK2VuAyEAj5qskz931xpwHXrN40pnxXSEz08Hxuhw2wABl+GG9yA=
-----END PUBLIC KEY-----
".ReplaceLineEndings();

         Assert.DoesNotThrow(() => PEMString.From(publicKey));
         PEMString validPemString = PEMString.From(publicKey);
         Assert.IsNotNull(validPemString);
         Assert.AreEqual(publicKey, validPemString.Value);

         bool shouldBeTrue = PEMString.TryFrom(publicKey, out var newValidPemString);
         Assert.IsTrue(shouldBeTrue);
         Assert.IsNotNull(newValidPemString);
         Assert.AreEqual(publicKey, newValidPemString.Value);
      }

      [Test]
      public void RSA_Keys_Are_Valid()
      {
         string rsaKey = @"-----BEGIN RSA PRIVATE KEY-----
MIIBOgIBAAJBAJCiNjSRqZgk+cAzf67t/wSFnFTTRQ5zlN4kbdOOceNEaEcEFgp+
SaEoWKOOsdQNUya+zQ3TeAytIjOT9lSErTkCAwEAAQJAAu3B9r0MsofB0Jj1CNwJ
ZLRMQYcjWBhSPKWqCAATrCQuky7IbnT/W1R5kInHNhIiRV+t7kmTeZMB76aCVlF8
vQIhANCjmyt3t+fpKcrUg8gLt8KsASOvIKFoE5qgL+KD/d8VAiEAsXciMeKy/6Tl
rG4Gq2AAv8mqEkB93ic/XGYmPOd//pUCIQCYR/HHxjfK4xoH2xjceAEF67lhLD+q
z2YPo/+PWzt/CQIgc4JolnHJMo6BE7+1xZxCQJMhiKnDg3KmUh0G7IN+ExUCIF5l
2zoR2BRJjNEpn4SSIuv1D87yFG8wlcgxeTCl1/yk
-----END RSA PRIVATE KEY-----".ReplaceLineEndings();

         Assert.DoesNotThrow(() => PEMString.From(rsaKey));
         PEMString validPemString = PEMString.From(rsaKey);
         Assert.IsNotNull(validPemString);
         Assert.AreEqual(rsaKey, validPemString.Value);

         bool shouldBeTrue = PEMString.TryFrom(rsaKey, out var newValidPemString);
         Assert.IsTrue(shouldBeTrue);
         Assert.IsNotNull(newValidPemString);
         Assert.AreEqual(rsaKey, newValidPemString.Value);
      }
   }
}

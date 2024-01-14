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
public class EmailAddress_Tests
{
    [TestCase(null)]
    public void Null_Email_Addresses_Are_Invalid(string emailAddress)
    {
        Assert.Throws<ValueNullException>(() => EmailAddress.From(emailAddress));

        bool shouldBeFalse = EmailAddress.TryFrom(emailAddress, out EmailAddress shouldBeNull);
        Assert.That(shouldBeFalse, Is.False);
        Assert.That(shouldBeNull, Is.Null);
    }

    [TestCase("")]
    [TestCase(" ")]
    [TestCase("   ")]
    public void Empty_Email_Addresses_Are_Invalid(string emailAddress)
    {
        Assert.Throws<ValueEmptyException>(() => EmailAddress.From(emailAddress));

        bool shouldBeFalse = EmailAddress.TryFrom(emailAddress, out EmailAddress shouldBeNull);
        Assert.That(shouldBeFalse, Is.False);
        Assert.That(shouldBeNull, Is.Null);
    }

    [TestCase("hello@crypter.dev.")]
    [TestCase("@crypter.dev")]
    public void Invalid_Email_Addresses_Are_Invalid(string emailAddress)
    {
        Assert.Throws<ValueInvalidException>(() => EmailAddress.From(emailAddress));

        bool shouldBeFalse = EmailAddress.TryFrom(emailAddress, out EmailAddress shouldBeNull);
        Assert.That(shouldBeFalse, Is.False);
        Assert.That(shouldBeNull, Is.Null);
    }

    [TestCase("jack@crypter.dev")]
    [TestCase("no-reply@crypter.dev")]
    [TestCase("anyone@gmail.com")]
    public void Valid_Email_Addresses_Are_Valid(string emailAddress)
    {
        Assert.DoesNotThrow(() => EmailAddress.From(emailAddress));
        EmailAddress validEmailAddress = EmailAddress.From(emailAddress);
        Assert.That(validEmailAddress, Is.Not.Null);
        Assert.That(validEmailAddress.Value, Is.EqualTo(emailAddress));

        bool shouldBeTrue = EmailAddress.TryFrom(emailAddress, out EmailAddress newValidEmailAddress);
        Assert.That(shouldBeTrue, Is.True);
        Assert.That(newValidEmailAddress, Is.Not.Null);
        Assert.That(newValidEmailAddress.Value, Is.EqualTo(emailAddress));
    }
}

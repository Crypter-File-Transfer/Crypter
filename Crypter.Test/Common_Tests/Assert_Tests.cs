/*
 * Copyright (C) 2026 Crypter File Transfer
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

using System.Threading.Tasks;
using EasyMonads;
using NUnit.Framework;

namespace Crypter.Test.Common_Tests;

/// <summary>
/// Every monad assertion is exercised twice: once against the state it expects, and once against a
/// state it must reject. A helper that never fails would silently pass every test that depends on it.
/// </summary>
/// <remarks>
/// The rejection tests wrap the assertion in <c>Assert.Throws</c> rather than a try/catch. NUnit
/// records a failed assertion against the running test as soon as it happens, and only
/// <c>Assert.Throws</c> clears that record. A caught <see cref="AssertionException"/> still fails the
/// test.
/// </remarks>
[TestFixture]
internal class Assert_Tests
{
    private const string SomeValue = "Frodo";
    private const string OtherValue = "Bilbo";
    private const int LeftValue = 42;
    private const int OtherLeftValue = 24;

    private static Maybe<string> Some => Maybe<string>.From(SomeValue);
    private static Maybe<string> None => Maybe<string>.None;

    private static Either<int, string> Right => Either<int, string>.FromRight(SomeValue);
    private static Either<int, string> Left => Either<int, string>.FromLeft(LeftValue);
    private static Either<int, string> Neither => Either<int, string>.Neither;

    [Test]
    public void Is_Some_Returns_The_Value()
    {
        string value = Assert.IsSome(Some);
        Assert.That(value, Is.EqualTo(SomeValue));
    }

    [Test]
    public void Is_Some_Fails_For_None()
    {
        AssertionException? exception = Assert.Throws<AssertionException>(() => Assert.IsSome(None));
        Assert.That(exception!.Message, Is.EqualTo("Expected Some, but was None."));
    }

    [Test]
    public void Is_Some_With_An_Expected_Value_Returns_The_Value()
    {
        string value = Assert.IsSome(Some, SomeValue);
        Assert.That(value, Is.EqualTo(SomeValue));
    }

    [Test]
    public void Is_Some_With_An_Expected_Value_Fails_For_A_Different_Value()
    {
        Assert.Throws<AssertionException>(() => Assert.IsSome(Some, OtherValue));
    }

    [Test]
    public void Is_None_Passes_For_None()
    {
        Assert.IsNone(None);
    }

    [Test]
    public void Is_None_Fails_For_Some()
    {
        AssertionException? exception = Assert.Throws<AssertionException>(() => Assert.IsNone(Some));
        Assert.That(exception!.Message, Is.EqualTo($"Expected None, but was Some({SomeValue})."));
    }

    [Test]
    public void Is_Right_Returns_The_Value()
    {
        string value = Assert.IsRight(Right);
        Assert.That(value, Is.EqualTo(SomeValue));
    }

    [Test]
    public void Is_Right_Fails_For_Left()
    {
        AssertionException? exception = Assert.Throws<AssertionException>(() => Assert.IsRight(Left));
        Assert.That(exception!.Message, Is.EqualTo($"Expected Right, but was Left({LeftValue})."));
    }

    [Test]
    public void Is_Right_Fails_For_Neither()
    {
        AssertionException? exception = Assert.Throws<AssertionException>(() => Assert.IsRight(Neither));
        Assert.That(exception!.Message, Is.EqualTo("Expected Right, but was Neither."));
    }

    [Test]
    public void Is_Right_With_An_Expected_Value_Returns_The_Value()
    {
        string value = Assert.IsRight(Right, SomeValue);
        Assert.That(value, Is.EqualTo(SomeValue));
    }

    [Test]
    public void Is_Right_With_An_Expected_Value_Fails_For_A_Different_Value()
    {
        Assert.Throws<AssertionException>(() => Assert.IsRight(Right, OtherValue));
    }

    [Test]
    public void Is_Left_Returns_The_Value()
    {
        int value = Assert.IsLeft(Left);
        Assert.That(value, Is.EqualTo(LeftValue));
    }

    [Test]
    public void Is_Left_Fails_For_Right()
    {
        AssertionException? exception = Assert.Throws<AssertionException>(() => Assert.IsLeft(Right));
        Assert.That(exception!.Message, Is.EqualTo($"Expected Left, but was Right({SomeValue})."));
    }

    [Test]
    public void Is_Left_Fails_For_Neither()
    {
        AssertionException? exception = Assert.Throws<AssertionException>(() => Assert.IsLeft(Neither));
        Assert.That(exception!.Message, Is.EqualTo("Expected Left, but was Neither."));
    }

    [Test]
    public void Is_Left_With_An_Expected_Value_Returns_The_Value()
    {
        int value = Assert.IsLeft(Left, LeftValue);
        Assert.That(value, Is.EqualTo(LeftValue));
    }

    [Test]
    public void Is_Left_With_An_Expected_Value_Fails_For_A_Different_Value()
    {
        Assert.Throws<AssertionException>(() => Assert.IsLeft(Left, OtherLeftValue));
    }

    [Test]
    public void Is_Neither_Passes_For_Neither()
    {
        Assert.IsNeither(Neither);
    }

    [Test]
    public void Is_Neither_Fails_For_Right()
    {
        AssertionException? exception = Assert.Throws<AssertionException>(() => Assert.IsNeither(Right));
        Assert.That(exception!.Message, Is.EqualTo($"Expected Neither, but was Right({SomeValue})."));
    }

    [Test]
    public void Is_Some_Inside_A_Multiple_Scope_Does_Not_Return_A_Value()
    {
        bool returned = false;

        Assert.Throws<AssertionException>(() => Assert.Multiple(() =>
        {
            _ = Assert.IsSome(None);
            returned = true;
        }));

        Assert.That(returned, Is.False);
    }

    [Test]
    public void Is_Right_Inside_A_Multiple_Scope_Does_Not_Return_A_Value()
    {
        bool returned = false;

        Assert.Throws<AssertionException>(() => Assert.Multiple(() =>
        {
            _ = Assert.IsRight(Left);
            returned = true;
        }));

        Assert.That(returned, Is.False);
    }

    [Test]
    public void Is_Left_Inside_A_Multiple_Scope_Does_Not_Return_A_Value()
    {
        bool returned = false;

        Assert.Throws<AssertionException>(() => Assert.Multiple(() =>
        {
            _ = Assert.IsLeft(Right);
            returned = true;
        }));

        Assert.That(returned, Is.False);
    }

    [Test]
    public async Task Is_Some_Async_Returns_The_Value_Async()
    {
        string value = await Assert.IsSomeAsync(Task.FromResult(Some));
        Assert.That(value, Is.EqualTo(SomeValue));
    }

    [Test]
    public void Is_Some_Async_Fails_For_None()
    {
        AssertionException? exception =
            Assert.ThrowsAsync<AssertionException>(() => Assert.IsSomeAsync(Task.FromResult(None)));
        Assert.That(exception!.Message, Is.EqualTo("Expected Some, but was None."));
    }

    [Test]
    public async Task Is_Some_Async_With_An_Expected_Value_Returns_The_Value_Async()
    {
        string value = await Assert.IsSomeAsync(Task.FromResult(Some), SomeValue);
        Assert.That(value, Is.EqualTo(SomeValue));
    }

    [Test]
    public void Is_Some_Async_With_An_Expected_Value_Fails_For_A_Different_Value()
    {
        Assert.ThrowsAsync<AssertionException>(() => Assert.IsSomeAsync(Task.FromResult(Some), OtherValue));
    }

    [Test]
    public async Task Is_None_Async_Passes_For_None_Async()
    {
        await Assert.IsNoneAsync(Task.FromResult(None));
    }

    [Test]
    public void Is_None_Async_Fails_For_Some()
    {
        AssertionException? exception =
            Assert.ThrowsAsync<AssertionException>(() => Assert.IsNoneAsync(Task.FromResult(Some)));
        Assert.That(exception!.Message, Is.EqualTo($"Expected None, but was Some({SomeValue})."));
    }

    [Test]
    public async Task Is_Right_Async_Returns_The_Value_Async()
    {
        string value = await Assert.IsRightAsync(Task.FromResult(Right));
        Assert.That(value, Is.EqualTo(SomeValue));
    }

    [Test]
    public void Is_Right_Async_Fails_For_Left()
    {
        AssertionException? exception =
            Assert.ThrowsAsync<AssertionException>(() => Assert.IsRightAsync(Task.FromResult(Left)));
        Assert.That(exception!.Message, Is.EqualTo($"Expected Right, but was Left({LeftValue})."));
    }

    [Test]
    public async Task Is_Right_Async_With_An_Expected_Value_Returns_The_Value_Async()
    {
        string value = await Assert.IsRightAsync(Task.FromResult(Right), SomeValue);
        Assert.That(value, Is.EqualTo(SomeValue));
    }

    [Test]
    public void Is_Right_Async_With_An_Expected_Value_Fails_For_A_Different_Value()
    {
        Assert.ThrowsAsync<AssertionException>(() => Assert.IsRightAsync(Task.FromResult(Right), OtherValue));
    }

    [Test]
    public async Task Is_Left_Async_Returns_The_Value_Async()
    {
        int value = await Assert.IsLeftAsync(Task.FromResult(Left));
        Assert.That(value, Is.EqualTo(LeftValue));
    }

    [Test]
    public void Is_Left_Async_Fails_For_Right()
    {
        AssertionException? exception =
            Assert.ThrowsAsync<AssertionException>(() => Assert.IsLeftAsync(Task.FromResult(Right)));
        Assert.That(exception!.Message, Is.EqualTo($"Expected Left, but was Right({SomeValue})."));
    }

    [Test]
    public async Task Is_Left_Async_With_An_Expected_Value_Returns_The_Value_Async()
    {
        int value = await Assert.IsLeftAsync(Task.FromResult(Left), LeftValue);
        Assert.That(value, Is.EqualTo(LeftValue));
    }

    [Test]
    public void Is_Left_Async_With_An_Expected_Value_Fails_For_A_Different_Value()
    {
        Assert.ThrowsAsync<AssertionException>(() => Assert.IsLeftAsync(Task.FromResult(Left), OtherLeftValue));
    }

    [Test]
    public async Task Is_Neither_Async_Passes_For_Neither_Async()
    {
        await Assert.IsNeitherAsync(Task.FromResult(Neither));
    }

    [Test]
    public void Is_Neither_Async_Fails_For_Left()
    {
        AssertionException? exception =
            Assert.ThrowsAsync<AssertionException>(() => Assert.IsNeitherAsync(Task.FromResult(Left)));
        Assert.That(exception!.Message, Is.EqualTo($"Expected Neither, but was Left({LeftValue})."));
    }
}

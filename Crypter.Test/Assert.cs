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

namespace Crypter.Test;

/// <summary>
/// NUnit's assertions plus assertions for <see cref="Maybe{T}"/> and <see cref="Either{TLeft,TRight}"/>.
/// </summary>
/// <remarks>
/// This type shadows <see cref="NUnit.Framework.Assert"/> for every test in the Crypter.Test namespace,
/// so the inherited assertions are reached through the same <c>Assert</c> name.
/// The monad assertions come in two forms, which fail in different ways. The <c>Is*</c> assertions
/// return nothing and report a wrong state through <c>Fail</c>, so an <c>Assert.Multiple</c> block
/// collects the failure and runs the statements after it. The <c>*ValueOf</c> assertions return the
/// matched value, which lets a test assert the state and bind the value in one statement; a wrong
/// state leaves them no value to return, so they throw an <see cref="AssertionException"/> and end an
/// <c>Assert.Multiple</c> block where they stand. Both forms check an expected value with <c>That</c>,
/// so a value that does not match is collected by an <c>Assert.Multiple</c> block either way.
/// Each state has one <c>Async</c> form, the one that returns what the state holds.
/// </remarks>
internal abstract class Assert : NUnit.Framework.Assert
{
    /// <summary>
    /// Assert that the maybe is in the Some state.
    /// </summary>
    internal static void IsSome<T>(Maybe<T> maybe)
    {
        if (maybe.IsNone)
        {
            Fail($"Expected Some, but was {Describe(maybe)}.");
        }
    }

    /// <summary>
    /// Assert that the maybe is in the Some state and holds <paramref name="expectedValue"/>.
    /// </summary>
    internal static void IsSome<T>(Maybe<T> maybe, T expectedValue)
    {
        IsSome(maybe);

        if (maybe.IsSome)
        {
            That(maybe.SomeOrDefault(), Is.EqualTo(expectedValue));
        }
    }

    /// <summary>
    /// Assert that the maybe is in the Some state and return the value it holds.
    /// </summary>
    internal static T SomeValueOf<T>(Maybe<T> maybe)
    {
        if (maybe.IsNone)
        {
            throw new AssertionException($"Expected Some, but was {Describe(maybe)}.");
        }

        return maybe.SomeOrDefault()!;
    }

    /// <summary>
    /// Assert that the maybe is in the Some state and holds <paramref name="expectedValue"/>, then
    /// return the value it holds.
    /// </summary>
    internal static T SomeValueOf<T>(Maybe<T> maybe, T expectedValue)
    {
        T value = SomeValueOf(maybe);
        That(value, Is.EqualTo(expectedValue));
        return value;
    }

    /// <summary>
    /// Assert that the maybe is in the None state.
    /// </summary>
    internal static void IsNone<T>(Maybe<T> maybe)
    {
        if (maybe.IsSome)
        {
            Fail($"Expected None, but was {Describe(maybe)}.");
        }
    }

    /// <summary>
    /// Assert that the either is in the Right state.
    /// </summary>
    internal static void IsRight<TLeft, TRight>(Either<TLeft, TRight> either)
    {
        if (!either.IsRight)
        {
            Fail($"Expected Right, but was {Describe(either)}.");
        }
    }

    /// <summary>
    /// Assert that the either is in the Right state and holds <paramref name="expectedValue"/>.
    /// </summary>
    internal static void IsRight<TLeft, TRight>(Either<TLeft, TRight> either, TRight expectedValue)
    {
        IsRight(either);

        if (either.IsRight)
        {
            That(either.RightOrDefault(default!), Is.EqualTo(expectedValue));
        }
    }

    /// <summary>
    /// Assert that the either is in the Right state and return the value it holds.
    /// </summary>
    internal static TRight RightValueOf<TLeft, TRight>(Either<TLeft, TRight> either)
    {
        if (!either.IsRight)
        {
            throw new AssertionException($"Expected Right, but was {Describe(either)}.");
        }

        return either.RightOrDefault(default!)!;
    }

    /// <summary>
    /// Assert that the either is in the Right state and holds <paramref name="expectedValue"/>, then
    /// return the value it holds.
    /// </summary>
    internal static TRight RightValueOf<TLeft, TRight>(Either<TLeft, TRight> either, TRight expectedValue)
    {
        TRight value = RightValueOf(either);
        That(value, Is.EqualTo(expectedValue));
        return value;
    }

    /// <summary>
    /// Assert that the either is in the Left state.
    /// </summary>
    internal static void IsLeft<TLeft, TRight>(Either<TLeft, TRight> either)
    {
        if (!either.IsLeft)
        {
            Fail($"Expected Left, but was {Describe(either)}.");
        }
    }

    /// <summary>
    /// Assert that the either is in the Left state and holds <paramref name="expectedValue"/>.
    /// </summary>
    internal static void IsLeft<TLeft, TRight>(Either<TLeft, TRight> either, TLeft expectedValue)
    {
        IsLeft(either);

        if (either.IsLeft)
        {
            That(either.LeftOrDefault(default!), Is.EqualTo(expectedValue));
        }
    }

    /// <summary>
    /// Assert that the either is in the Left state and return the value it holds.
    /// </summary>
    internal static TLeft LeftValueOf<TLeft, TRight>(Either<TLeft, TRight> either)
    {
        if (!either.IsLeft)
        {
            throw new AssertionException($"Expected Left, but was {Describe(either)}.");
        }

        return either.LeftOrDefault(default!)!;
    }

    /// <summary>
    /// Assert that the either is in the Left state and holds <paramref name="expectedValue"/>, then
    /// return the value it holds.
    /// </summary>
    internal static TLeft LeftValueOf<TLeft, TRight>(Either<TLeft, TRight> either, TLeft expectedValue)
    {
        TLeft value = LeftValueOf(either);
        That(value, Is.EqualTo(expectedValue));
        return value;
    }

    /// <summary>
    /// Assert that the either is in the Neither state.
    /// </summary>
    internal static void IsNeither<TLeft, TRight>(Either<TLeft, TRight> either)
    {
        if (!either.IsNeither)
        {
            Fail($"Expected Neither, but was {Describe(either)}.");
        }
    }

    /// <summary>
    /// Await the task, then assert that the maybe is in the Some state and return the value it holds.
    /// </summary>
    internal static async Task<T> SomeValueOfAsync<T>(Task<Maybe<T>> maybeTask)
    {
        return SomeValueOf(await maybeTask);
    }

    /// <summary>
    /// Await the task, then assert that the maybe is in the Some state and holds
    /// <paramref name="expectedValue"/>, then return the value it holds.
    /// </summary>
    internal static async Task<T> SomeValueOfAsync<T>(Task<Maybe<T>> maybeTask, T expectedValue)
    {
        return SomeValueOf(await maybeTask, expectedValue);
    }

    /// <summary>
    /// Await the task, then assert that the maybe is in the None state.
    /// </summary>
    internal static async Task IsNoneAsync<T>(Task<Maybe<T>> maybeTask)
    {
        IsNone(await maybeTask);
    }

    /// <summary>
    /// Await the task, then assert that the either is in the Right state and return the value it holds.
    /// </summary>
    internal static async Task<TRight> RightValueOfAsync<TLeft, TRight>(Task<Either<TLeft, TRight>> eitherTask)
    {
        return RightValueOf(await eitherTask);
    }

    /// <summary>
    /// Await the task, then assert that the either is in the Right state and holds
    /// <paramref name="expectedValue"/>, then return the value it holds.
    /// </summary>
    internal static async Task<TRight> RightValueOfAsync<TLeft, TRight>(Task<Either<TLeft, TRight>> eitherTask, TRight expectedValue)
    {
        return RightValueOf(await eitherTask, expectedValue);
    }

    /// <summary>
    /// Await the task, then assert that the either is in the Left state and return the value it holds.
    /// </summary>
    internal static async Task<TLeft> LeftValueOfAsync<TLeft, TRight>(Task<Either<TLeft, TRight>> eitherTask)
    {
        return LeftValueOf(await eitherTask);
    }

    /// <summary>
    /// Await the task, then assert that the either is in the Left state and holds
    /// <paramref name="expectedValue"/>, then return the value it holds.
    /// </summary>
    internal static async Task<TLeft> LeftValueOfAsync<TLeft, TRight>(Task<Either<TLeft, TRight>> eitherTask, TLeft expectedValue)
    {
        return LeftValueOf(await eitherTask, expectedValue);
    }

    /// <summary>
    /// Await the task, then assert that the either is in the Neither state.
    /// </summary>
    internal static async Task IsNeitherAsync<TLeft, TRight>(Task<Either<TLeft, TRight>> eitherTask)
    {
        IsNeither(await eitherTask);
    }

    private static string Describe<T>(Maybe<T> maybe)
    {
        return maybe.IsSome
            ? $"Some({maybe.SomeOrDefault()})"
            : "None";
    }

    private static string Describe<TLeft, TRight>(Either<TLeft, TRight> either)
    {
        if (either.IsRight)
        {
            return $"Right({either.RightOrDefault(default!)})";
        }

        return either.IsLeft
            ? $"Left({either.LeftOrDefault(default!)})"
            : "Neither";
    }
}

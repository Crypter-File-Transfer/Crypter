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

using System;
using System.Threading.Tasks;

namespace Crypter.Common.Monads
{
   public static class MaybeAsyncExtensions
   {
      public static async Task<TResult> MatchAsync<TValue, TResult>(this Task<Maybe<TValue>> maybe, Func<TResult> none, Func<TValue, TResult> some)
      {
         Maybe<TValue> maybeResult = await maybe;
         return maybeResult.Match(none, some);
      }

      public static async Task<TResult> MatchAsync<TValue, TResult>(this Task<Maybe<TValue>> maybe, Func<TResult> none, Func<TValue, Task<TResult>> someAsync)
      {
         Maybe<TValue> maybeResult = await maybe;
         return await maybeResult.MatchAsync(none, someAsync);
      }

      public static async Task<TResult> MatchAsync<TValue, TResult>(this Task<Maybe<TValue>> maybe, Func<Task<TResult>> noneAsync, Func<TValue, TResult> some)
      {
         Maybe<TValue> maybeResult = await maybe;
         return await maybeResult.MatchAsync(noneAsync, some);
      }

      public static Task<TValue> SomeOrDefaultAsync<TValue>(this Task<Maybe<TValue>> maybe, TValue defaultValue)
      {
         return maybe.MatchAsync(
            () => defaultValue,
            x => x);
      }

      public static async Task<Unit> IfSomeAsync<TValue>(this Task<Maybe<TValue>> maybe, Func<TValue, Task> someAsync)
      {
         Maybe<TValue> maybeResult = await maybe;
         return await maybeResult.IfSomeAsync(someAsync);
      }

      public static Task<Maybe<TResult>> BindAsync<TValue, TResult>(this Task<Maybe<TValue>> maybe, Func<TValue, TResult> bind)
      {
         return maybe.MatchAsync(
            () => Maybe<TResult>.None,
            value => bind(value));
      }

      public static Task<Maybe<TResult>> BindAsync<TValue, TResult>(this Task<Maybe<TValue>> maybe, Func<TValue, Task<Maybe<TResult>>> bindAsync)
      {
         return maybe.MatchAsync(
            () => Maybe<TResult>.None,
            value => bindAsync(value));
      }

      public static Task<Either<TLeft, TValue>> ToEitherAsync<TLeft, TValue>(this Task<Maybe<TValue>> maybe, TLeft left)
      {
         return maybe.MatchAsync(
            () => left,
            value => Either<TLeft, TValue>.FromRight(value));
      }

      public static Task<Either<TValue, TRight>> ToLeftEitherAsync<TValue, TRight>(this Task<Maybe<TValue>> maybe, TRight right)
      {
         return maybe.MatchAsync(
            () => right,
            value => Either<TValue, TRight>.FromLeft(value));
      }

      public static Task<Maybe<TResult>> Select<TValue, TResult>(this Task<Maybe<TValue>> maybe, Func<TValue, TResult> map)
      {
         return maybe.MatchAsync(
            () => Maybe<TResult>.None,
            value => map(value));
      }

      public static Task<Maybe<TResult>> SelectMany<TValue, TIntermediate, TResult>(this Task<Maybe<TValue>> maybe, Func<TValue, Maybe<TIntermediate>> bind, Func<TValue, TIntermediate, TResult> project)
      {
         return maybe.MatchAsync(
            () => Maybe<TResult>.None,
            value =>
            {
               return bind(value).Match(
                  () => default,
                  intermediate =>
                  {
                     return project(value, intermediate);
                  });
            });
      }

      public static Task<Maybe<TValue>> Where<TValue>(this Task<Maybe<TValue>> maybe, Func<TValue, bool> predicate)
      {
         return maybe.MatchAsync(
            () => Maybe<TValue>.None,
            value =>
            {
               return predicate(value)
                  ? value
                  : Maybe<TValue>.None;
            });
      }
   }
}

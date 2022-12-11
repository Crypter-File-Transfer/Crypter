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

using System;
using System.Threading.Tasks;

namespace Crypter.Common.Monads
{
   public static class EitherAsyncExtensions
   {
      public static async Task<TResult> MatchAsync<TLeft, TRight, TResult>(this Task<Either<TLeft, TRight>> either, TResult leftOrNeither, Func<TRight, TResult> right)
      {
         Either<TLeft, TRight> eitherResult = await either;
         return eitherResult.Match(leftOrNeither, right);
      }

      public static async Task<TResult> MatchAsync<TLeft, TRight, TResult>(this Task<Either<TLeft, TRight>> either, Func<TLeft, TResult> left, Func<TRight, TResult> right, TResult neither)
      {
         Either<TLeft, TRight> eitherResult = await either;
         return eitherResult.Match(left, right, neither);
      }

      public static async Task<TResult> MatchAsync<TLeft, TRight, TResult>(this Task<Either<TLeft, TRight>> either, Func<TLeft, TResult> left, Func<TRight, Task<TResult>> rightAsync, TResult neither)
      {
         Either<TLeft, TRight> eitherResult = await either;
         return await eitherResult.MatchAsync(left, rightAsync, neither);
      }

      public static async Task<TResult> MatchAsync<TLeft, TRight, TResult>(this Task<Either<TLeft, TRight>> either, Func<TLeft, Task<TResult>> leftAsync, Func<TRight, TResult> right, TResult neither)
      {
         Either<TLeft, TRight> eitherResult = await either;
         return await eitherResult.MatchAsync(leftAsync, right, neither);
      }

      public static async Task<Unit> DoRightAsync<TLeft, TRight>(this Task<Either<TLeft, TRight>> either, Action<TRight> right)
      {
         var eitherResult = await either;
         return eitherResult.DoRight(right);
      }

      public async static Task<Unit> DoRightAsync<TLeft, TRight>(this Task<Either<TLeft, TRight>> either, Func<TRight, Task> rightAsync)
      {
         var eitherResult = await either;
         return await eitherResult.DoRightAsync(rightAsync);
      }

      public static Task<Either<TLeft, TResult>> MapAsync<TLeft, TRight, TResult>(this Task<Either<TLeft, TRight>> either, Func<TRight, Either<TLeft, TResult>> map)
      {
         return either.MatchAsync(
            left: left => Either<TLeft, TResult>.FromLeft(left),
            right: right => map(right),
            neither: Either<TLeft, TResult>.Neither);
      }

      public static Task<Either<TLeft, TResult>> MapAsync<TLeft, TRight, TResult>(this Task<Either<TLeft, TRight>> either, Func<TRight, Task<Either<TLeft, TResult>>> map)
      {
         return either.MatchAsync(
            left: left => Either<TLeft, TResult>.FromLeft(left),
            rightAsync: right => map(right),
            neither: Either<TLeft, TResult>.Neither);
      }

      public static Task<Either<TLeft, TResult>> BindAsync<TLeft, TRight, TResult>(this Task<Either<TLeft, TRight>> either, Func<TRight, Task<Either<TLeft, TResult>>> bind)
      {
         return either.MapAsync(
                async right =>
                    await Either<TLeft, TRight>.FromRight(right).MatchAsync(
                       left => Either<TLeft, TResult>.FromLeft(left),
                       async right2 => await bind(right2),
                       Either<TLeft, TResult>.Neither));
      }

      public static Task<Either<TLeft, TResult>> BindAsync<TLeft, TRight, TResult>(this Task<Either<TLeft, TRight>> either, Func<TRight, Either<TLeft, TResult>> bind)
      {
         return either.MapAsync(
                right =>
                    Either<TLeft, TRight>.FromRight(right).Match(
                       left => Either<TLeft, TResult>.FromLeft(left),
                       right2 => bind(right2),
                       Either<TLeft, TResult>.Neither));
      }

      public static Task<Maybe<TRight>> ToMaybeTask<TLeft, TRight>(this Task<Either<TLeft, TRight>> either)
      {
         return either.MatchAsync(
            left => Maybe<TRight>.None,
            right => right,
            Maybe<TRight>.None);
      }

      public static async Task<Either<TLeft, TResult>> Select<TLeft, TRight, TResult>(this Task<Either<TLeft, TRight>> either, Func<TRight, TResult> map)
      {
         return await either.MatchAsync(
            Either<TLeft, TResult>.FromLeft,
            right => map(right),
            Either<TLeft, TResult>.Neither);
      }

      public static async Task<Either<TLeft, TResult>> SelectMany<TLeft, TRight, TIntermediate, TResult>(this Task<Either<TLeft, TRight>> either, Func<TRight, Task<Either<TLeft, TIntermediate>>> bind, Func<TRight, TIntermediate, TResult> project)
      {
         return await either.BindAsync(async (TRight right) => 
            await bind(right).BindAsync(delegate (TIntermediate intermediate)
            {
               Either<TLeft, TResult> projection = project(right, intermediate);
               return projection.AsTask();
            }));
      }

      public static async Task<Either<TLeft, TRight>> Where<TLeft, TRight>(this Task<Either<TLeft, TRight>> either, Func<TRight, bool> predicate)
      {
         return await either.MatchAsync(
            left => Either<TLeft, TRight>.Neither,
            right =>
            {
               return predicate(right)
                  ? right
                  : Either<TLeft, TRight>.FromRight(right);
            },
            Either<TLeft, TRight>.Neither);
      }
   }
}

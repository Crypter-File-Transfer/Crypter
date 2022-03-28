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
   internal enum MaybeState
   {
      None,
      Some
   }

   public struct Maybe<TValue>
   {
      private readonly MaybeState _state;
      private readonly TValue _value;

      public Maybe()
      {
         _state = MaybeState.None;
         _value = default;
      }

      public Maybe(TValue value)
      {
         _value = value;
         _state = value is null
            ? MaybeState.None
            : MaybeState.Some;
      }

      public bool IsNone
      { get { return _state == MaybeState.None; } }

      public bool IsSome
      { get { return _state == MaybeState.Some; } }

      public void IfSome(Action<TValue> someAction)
      {
         if (someAction is null)
         {
            throw new ArgumentNullException(nameof(someAction));
         }

         if (IsSome)
         {
            someAction(_value);
         }
      }

      public async Task IfSomeAsync(Func<TValue, Task> someTask)
      {
         if (someTask is null)
         {
            throw new ArgumentNullException(nameof(someTask));
         }

         if (IsSome)
         {
            await someTask(_value);
         }
      }

      public void IfNone(Action noneAction)
      {
         if (noneAction is null)
         {
            throw new ArgumentNullException(nameof(noneAction));
         }

         if (IsNone)
         {
            noneAction();
         }
      }

      public TResult Match<TResult>(Func<TResult> noneFunction, Func<TValue, TResult> someFunction)
      {
         if (noneFunction is null)
         {
            throw new ArgumentNullException(nameof(noneFunction));
         }

         if (someFunction is null)
         {
            throw new ArgumentNullException(nameof(someFunction));
         }

         return IsSome
            ? someFunction(_value)
            : noneFunction();
      }

      public async Task<TResult> MatchAsync<TResult>(Func<TResult> noneFunction, Func<TValue, Task<TResult>> someTask)
      {
         if (noneFunction is null)
         {
            throw new ArgumentNullException(nameof(noneFunction));
         }

         if (someTask is null)
         {
            throw new ArgumentNullException(nameof(someTask));
         }

         return IsSome
            ? await someTask(_value)
            : noneFunction();
      }

      public async Task<TResult> MatchAsync<TResult>(Func<Task<TResult>> noneTask, Func<TValue, TResult> someFunction)
      {
         if (noneTask is null)
         {
            throw new ArgumentNullException(nameof(noneTask));
         }

         if (someFunction is null)
         {
            throw new ArgumentNullException(nameof(someFunction));
         }

         return IsSome
            ? someFunction(_value)
            : await noneTask();
      }

      public Maybe<TResult> Map<TResult>(Func<TValue, TResult> mapFunction)
      {
         if (mapFunction is null)
         {
            throw new ArgumentNullException(nameof(mapFunction));
         }

         return IsSome
            ? mapFunction(_value)
            : Maybe<TResult>.None;
      }

      public async Task<Maybe<TResult>> MapAsync<TResult>(Func<TValue, Task<TResult>> mapTask)
      {
         if (mapTask is null)
         {
            throw new ArgumentNullException(nameof(mapTask));
         }

         return IsSome
            ? await mapTask(_value)
            : Maybe<TResult>.None;
      }

      public Maybe<TResult> Bind<TResult>(Func<TValue, Maybe<TResult>> bindFunction)
      {
         if (bindFunction is null)
         {
            throw new ArgumentNullException(nameof(bindFunction));
         }

         return IsSome
            ? bindFunction(_value)
            : Maybe<TResult>.None;
      }

      public async Task<Maybe<TResult>> BindAsync<TResult>(Func<TValue, Task<Maybe<TResult>>> bindTask)
      {
         if (bindTask is null)
         {
            throw new ArgumentNullException(nameof(bindTask));
         }

         return IsSome
            ? await bindTask(_value)
            : Maybe<TResult>.None;
      }

      /// <summary>
      /// Unsafe!
      /// </summary>
      /// <param name="defaultValue"></param>
      /// <returns></returns>
      public TValue SomeOrDefault(TValue defaultValue = default)
      {
         return IsSome
            ? _value
            : defaultValue;
      }

      public static Maybe<TValue> None => new();

      public static implicit operator Maybe<TValue>(TValue value) => new(value);
   }
}

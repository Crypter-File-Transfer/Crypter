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

using Crypter.Common.Exceptions;
using System;
using System.Threading.Tasks;

namespace Crypter.Common.Monads
{
   public enum EitherState
   {
      Bottom,
      Left,
      Right
   }

   public struct Either<TLeft, TRight>
   {
      private readonly TLeft _left;
      private readonly TRight _right;
      private readonly EitherState _state;

      public Either()
      {
         _state = EitherState.Bottom;
         _left = default;
         _right = default;
      }

      public Either(TLeft left)
      {
         _state = EitherState.Left;
         _left = left;
         _right = default;
      }

      public Either(TRight right)
      {
         _state = EitherState.Right;
         _right = right;
         _left = default;
      }

      public bool IsLeft
      { get { return _state == EitherState.Left; } }

      public bool IsRight
      { get { return _state == EitherState.Right; } }

      public bool IsBottom
      { get { return _state == EitherState.Bottom; } }

      public TLeft LeftUnsafe
      {
         get
         {
            return IsLeft
               ? _left
               : throw new WrongMonadStateException();
         }
      }

      public TRight RightUnsafe
      {
         get
         {
            return IsRight
               ? _right
               : throw new WrongMonadStateException();
         }
      }

      private static void ValidateMatch<TL, TR>(Func<TLeft, TL> leftFunction, Func<TRight, TR> rightFunction)
      {
         if (leftFunction is null)
         {
            throw new ArgumentNullException(nameof(leftFunction));
         }

         if (rightFunction is null)
         {
            throw new ArgumentNullException(nameof(rightFunction));
         }
      }

      private static TResult MatchBottom<TResult>(Func<TResult> bottomFunction = null)
      {
         return bottomFunction is null
               ? default
               : bottomFunction();
      }

      public TResult Match<TResult>(Func<TLeft, TResult> leftFunction, Func<TRight, TResult> rightFunction, Func<TResult> bottomFunction = null)
      {
         ValidateMatch(leftFunction, rightFunction);

         return _state switch
         {
            EitherState.Bottom => MatchBottom(bottomFunction),
            EitherState.Left => leftFunction(_left),
            EitherState.Right => rightFunction(_right),
            _ => throw new NotImplementedException()
         };
      }

      public async Task<TResult> MatchAsync<TResult>(Func<TLeft, Task<TResult>> leftTask, Func<TRight, TResult> rightFunction, Func<TResult> bottomFunction = null)
      {
         ValidateMatch(leftTask, rightFunction);

         return _state switch
         {
            EitherState.Bottom => MatchBottom(bottomFunction),
            EitherState.Left => await leftTask(_left),
            EitherState.Right => rightFunction(_right),
            _ => throw new NotImplementedException()
         };
      }

      public async Task<TResult> MatchAsync<TResult>(Func<TLeft, TResult> leftFunction, Func<TRight, Task<TResult>> rightTask, Func<TResult> bottomFunction = null)
      {
         ValidateMatch(leftFunction, rightTask);

         return _state switch
         {
            EitherState.Bottom => MatchBottom(bottomFunction),
            EitherState.Left => leftFunction(_left),
            EitherState.Right => await rightTask(_right),
            _ => throw new NotImplementedException()
         };
      }

      public async Task<TResult> MatchAsync<TResult>(Func<TLeft, Task<TResult>> leftTask, Func <TRight, Task<TResult>> rightTask, Func<TResult> bottomFunction = null)
      {
         ValidateMatch(leftTask, rightTask);

         return _state switch
         {
            EitherState.Bottom => MatchBottom(bottomFunction),
            EitherState.Left => await leftTask(_left),
            EitherState.Right => await rightTask(_right),
            _ => throw new NotImplementedException()
         };
      }

      public Either<TLeft, TResult> Map<TResult>(Func<TRight, TResult> mapFunction)
      {
         if (mapFunction is null)
         {
            throw new ArgumentNullException(nameof(mapFunction));
         }

         return IsRight
            ? mapFunction(_right)
            : new Either<TLeft, TResult>(_left);
      }

      public async Task<Either<TLeft, TResult>> MapAsync<TResult>(Func<TRight, Task<TResult>> mapTask)
      {
         if (mapTask is null)
         {
            throw new ArgumentNullException(nameof(mapTask));
         }

         return IsRight
            ? await mapTask(_right)
            : new Either<TLeft, TResult>(_left);
      }

      public Either<TLeft, TResult> Bind<TResult>(Func<TRight, Either<TLeft, TResult>> bindFunction)
      {
         if (bindFunction is null)
         {
            throw new ArgumentNullException(nameof(bindFunction));
         }

         return IsRight
            ? bindFunction(_right)
            : new Either<TLeft, TResult>(_left);
      }

      public async Task<Either<TLeft, TResult>> BindAsync<TResult>(Func<TRight, Task<Either<TLeft, TResult>>> bindTask)
      {
         if (bindTask is null)
         {
            throw new ArgumentNullException(nameof(bindTask));
         }

         return IsRight
            ? await bindTask(_right)
            : new Either<TLeft, TResult>(_left);
      }

      public void DoRight(Action<TRight> rightAction)
      {
         if (rightAction is null)
         {
            throw new ArgumentNullException(nameof(rightAction));
         }

         if (IsRight)
         {
            rightAction(_right);
         }
      }

      public async Task DoRightAsync(Func<TRight, Task> rightTask)
      {
         if (rightTask is null)
         {
            throw new ArgumentNullException(nameof(rightTask));
         }

         if (IsRight)
         {
            await rightTask(_right);
         }
      }

      public void DoLeft(Action<TLeft> leftAction)
      {
         if (leftAction is null)
         {
            throw new ArgumentNullException(nameof(leftAction));
         }

         if (IsLeft)
         {
            leftAction(_left);
         }
      }

      public async Task DoLeftAsync(Func<TLeft, Task> leftTask)
      {
         if (leftTask is null)
         {
            throw new ArgumentNullException(nameof(leftTask));
         }

         if (IsLeft)
         {
            await leftTask(_left);
         }
      }

      public static implicit operator Either<TLeft, TRight>(TLeft left) => new(left);

      public static implicit operator Either<TLeft, TRight>(TRight right) => new(right);
   }
}

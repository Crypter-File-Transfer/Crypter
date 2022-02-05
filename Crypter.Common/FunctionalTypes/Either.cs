/*
 * Copyright (C) 2021 Crypter File Transfer
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
 * Contact the current copyright holder to discuss commerical license options.
 */

using System;
using System.Threading.Tasks;

namespace Crypter.Common.FunctionalTypes
{
   public enum EitherState
   {
      Bottom,
      Left,
      Right
   }

   public class Either<TLeft, TRight>
   {
      private readonly TLeft _left;
      private readonly TRight _right;
      private readonly EitherState _state;

      public Either()
      {
         _state = EitherState.Bottom;
      }

      public Either(TLeft left)
      {
         _left = left;
         _state = EitherState.Left;
      }

      public Either(TRight right)
      {
         _right = right;
         _state = EitherState.Right;
      }

      public bool IsLeft
      { get { return _state == EitherState.Left; } }

      public bool IsRight
      { get { return _state == EitherState.Right; } }

      public bool IsBottom
      { get { return _state == EitherState.Bottom; } }

      public T Match<T>(Func<TLeft, T> leftFunction, Func<TRight, T> rightFunction, Func<T> bottomFunction = null)
      {
         if (leftFunction == null)
         {
            throw new ArgumentNullException(nameof(leftFunction));
         }

         if (rightFunction == null)
         {
            throw new ArgumentNullException(nameof(rightFunction));
         }

         T handleBottomFunction()
         {
            return bottomFunction is null
               ? default
               : bottomFunction();
         }

         return _state switch
         {
            EitherState.Bottom => handleBottomFunction(),
            EitherState.Left => leftFunction(_left),
            EitherState.Right => rightFunction(_right),
            _ => throw new NotImplementedException()
         };
      }

      public async Task<T> MatchAsync<T>(Func<TLeft, Task<T>> leftFunction, Func<TRight, Task<T>> rightFunction, Func<Task<T>> bottomFunction = null)
      {
         if (leftFunction == null)
         {
            throw new ArgumentNullException(nameof(leftFunction));
         }

         if (rightFunction == null)
         {
            throw new ArgumentNullException(nameof(rightFunction));
         }

         async Task<T> handleBottomFunction()
         {
            return bottomFunction is null
               ? await Task.FromResult<T>(default)
               : await bottomFunction();
         }

         return _state switch
         {
            EitherState.Bottom => await handleBottomFunction(),
            EitherState.Left => await leftFunction(_left),
            EitherState.Right => await rightFunction(_right),
            _ => throw new NotImplementedException()
         };
      }

      public void MatchVoid(Action<TLeft> leftAction, Action<TRight> rightAction, Action bottomAction = null)
      {
         if (leftAction == null)
         {
            throw new ArgumentNullException(nameof(leftAction));
         }

         if (rightAction == null)
         {
            throw new ArgumentNullException(nameof(rightAction));
         }

         switch (_state)
         {
            case EitherState.Bottom:
               bottomAction?.Invoke();
               break;
            case EitherState.Left:
               leftAction(_left);
               break;
            case EitherState.Right:
               rightAction(_right);
               break;
            default:
               throw new NotImplementedException();
         }
      }

      public async Task MatchVoidAsync(Func<TLeft, Task> leftFunction, Func<TRight, Task> rightFunction, Func<Task> bottomFunction = null)
      {
         if (leftFunction == null)
         {
            throw new ArgumentNullException(nameof(leftFunction));
         }

         if (rightFunction == null)
         {
            throw new ArgumentNullException(nameof(rightFunction));
         }

         switch (_state)
         {
            case EitherState.Bottom:
               await bottomFunction?.Invoke();
               break;
            case EitherState.Left:
               await leftFunction(_left);
               break;
            case EitherState.Right:
               await rightFunction(_right);
               break;
            default:
               throw new NotImplementedException();
         }
      }

      public void DoRight(Action<TRight> rightAction)
      {
         if (rightAction == null)
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
         if (rightTask == null)
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
         if (leftAction == null)
         {
            throw new ArgumentNullException(nameof(leftAction));
         }

         if (IsLeft)
         {
            leftAction(_left);
         }
      }

      public static implicit operator Either<TLeft, TRight>(TLeft left) => new Either<TLeft, TRight>(left);

      public static implicit operator Either<TLeft, TRight>(TRight right) => new Either<TLeft, TRight>(right);
   }
}

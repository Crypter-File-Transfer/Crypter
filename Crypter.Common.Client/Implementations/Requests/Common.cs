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

using Crypter.Common.Contracts;
using Crypter.Common.Monads;
using System.Linq;
using System.Threading.Tasks;

namespace Crypter.Common.Client.Implementations.Requests
{
   internal static class Common
   {
      /// <summary>
      /// Lift the first error code out of the API error response.
      /// </summary>
      /// <typeparam name="TErrorCode"></typeparam>
      /// <typeparam name="TResponse"></typeparam>
      /// <param name="response"></param>
      /// <returns></returns>
      /// <remarks>
      /// Need to refactor Crypter.Web and other client services to handle multiple error codes.
      /// </remarks>
      internal static Either<TErrorCode, TResponse> ExtractErrorCode<TErrorCode, TResponse>(Either<ErrorResponse, TResponse> response)
      {
         return response
            .MapLeft(x => x.Errors.Select(y => (TErrorCode)(object)y.ErrorCode).First());
      }

      /// <summary>
      /// Lift the first error code out of the API error response.
      /// </summary>
      /// <typeparam name="TErrorCode"></typeparam>
      /// <typeparam name="TResponse"></typeparam>
      /// <param name="response"></param>
      /// <returns></returns>
      /// <remarks>
      /// Need to refactor Crypter.Web and other client services to handle multiple error codes.
      /// </remarks>
      internal static Task<Either<TErrorCode, TResponse>> ExtractErrorCode<TErrorCode, TResponse>(this Task<Either<ErrorResponse, TResponse>> response)
      {
         return response
            .MapLeftAsync<ErrorResponse, TResponse, TErrorCode>(x => x.Errors.Select(y => (TErrorCode)(object)y.ErrorCode).First());
      }
   }
}

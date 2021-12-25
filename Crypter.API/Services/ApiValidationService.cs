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

using Crypter.Common.Services;
using Crypter.Contracts.Enum;
using Crypter.Contracts.Requests;
using Crypter.Core.Interfaces;
using Crypter.Core.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.API.Services
{
   public interface IApiValidationService
   {
      Task<bool> IsEnoughSpaceForNewTransferAsync(long allocatedDiskSpace, int maxUploadSize, CancellationToken cancellationToken);
      Task<InsertUserResult> IsValidUserRegistrationRequestAsync(RegisterUserRequest request, CancellationToken cancellationToken);
   }

   public class ApiValidationService : IApiValidationService
   {
      private readonly IUserService UserService;
      private readonly IBaseTransferService<MessageTransfer> MessageTransferService;
      private readonly IBaseTransferService<FileTransfer> FileTransferService;

      public ApiValidationService(IUserService userService, IBaseTransferService<MessageTransfer> messageTransferService, IBaseTransferService<FileTransfer> fileTransferService)
      {
         UserService = userService;
         MessageTransferService = messageTransferService;
         FileTransferService = fileTransferService;
      }

      public async Task<bool> IsEnoughSpaceForNewTransferAsync(long allocatedDiskSpace, int maxUploadSize, CancellationToken cancellationToken)
      {
         var sizeOfFileUploads = await MessageTransferService.GetAggregateSizeAsync(cancellationToken);
         var sizeOfMessageUploads = await FileTransferService.GetAggregateSizeAsync(cancellationToken);
         var totalSizeOfUploads = sizeOfFileUploads + sizeOfMessageUploads;
         return (totalSizeOfUploads + maxUploadSize) <= allocatedDiskSpace;
      }

      public async Task<InsertUserResult> IsValidUserRegistrationRequestAsync(RegisterUserRequest request, CancellationToken cancellationToken)
      {
         if (!ValidationService.IsValidUsername(request.Username))
         {
            return InsertUserResult.InvalidUsername;
         }

         if (!ValidationService.IsValidPassword(request.Password))
         {
            return InsertUserResult.InvalidPassword;
         }

         if (ValidationService.IsPossibleEmailAddress(request.Email)
            && !ValidationService.IsValidEmailAddress(request.Email))
         {
            return InsertUserResult.InvalidEmailAddress;
         }

         if (!await UserService.IsUsernameAvailableAsync(request.Username, cancellationToken))
         {
            return InsertUserResult.UsernameTaken;
         }

         if (ValidationService.IsPossibleEmailAddress(request.Email)
            && !await UserService.IsEmailAddressAvailableAsync(request.Email, cancellationToken))
         {
            return InsertUserResult.EmailTaken;
         }

         return InsertUserResult.Success;
      }
   }
}

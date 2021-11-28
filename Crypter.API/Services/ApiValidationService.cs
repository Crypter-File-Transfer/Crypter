using Crypter.Common.Services;
using Crypter.Contracts.Enum;
using Crypter.Contracts.Requests;
using Crypter.Core.Interfaces;
using Crypter.Core.Models;
using System.Threading.Tasks;

namespace Crypter.API.Services
{
   public interface IApiValidationService
   {
      Task<bool> IsEnoughSpaceForNewTransfer(long allocatedDiskSpace, int maxUploadSize);
      Task<InsertUserResult> IsValidUserRegistrationRequest(RegisterUserRequest request);
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

      public async Task<bool> IsEnoughSpaceForNewTransfer(long allocatedDiskSpace, int maxUploadSize)
      {
         var sizeOfFileUploads = await MessageTransferService.GetAggregateSizeAsync();
         var sizeOfMessageUploads = await FileTransferService.GetAggregateSizeAsync();
         var totalSizeOfUploads = sizeOfFileUploads + sizeOfMessageUploads;
         return (totalSizeOfUploads + maxUploadSize) <= allocatedDiskSpace;
      }

      public async Task<InsertUserResult> IsValidUserRegistrationRequest(RegisterUserRequest request)
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

         if (!await UserService.IsUsernameAvailableAsync(request.Username))
         {
            return InsertUserResult.UsernameTaken;
         }

         if (ValidationService.IsPossibleEmailAddress(request.Email)
            && !await UserService.IsEmailAddressAvailableAsync(request.Email))
         {
            return InsertUserResult.EmailTaken;
         }

         return InsertUserResult.Success;
      }
   }
}

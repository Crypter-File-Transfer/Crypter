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

using Crypter.API.Controllers.Methods;
using Crypter.API.Services;
using Crypter.Common.Services;
using Crypter.Contracts.DTO;
using Crypter.Contracts.Enum;
using Crypter.Contracts.Requests;
using Crypter.Contracts.Responses;
using Crypter.Core.Features.User.Commands;
using Crypter.Core.Features.User.Queries;
using Crypter.Core.Interfaces;
using Crypter.Core.Models;
using Crypter.CryptoLib;
using Crypter.CryptoLib.Crypto;
using Hangfire;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.API.Controllers
{
   [ApiController]
   [Route("api/user")]
   public class UserController : ControllerBase
   {
      private readonly IUserService _userService;
      private readonly IUserProfileService _userProfileService;
      private readonly IUserPublicKeyPairService<UserX25519KeyPair> _userX25519KeyPairService;
      private readonly IUserPublicKeyPairService<UserEd25519KeyPair> _userEd25519KeyPairService;
      private readonly IUserSearchService _userSearchService;
      private readonly IUserPrivacySettingService _userPrivacySettingService;
      private readonly IUserEmailVerificationService _userEmailVerificationService;
      private readonly IUserNotificationSettingService _userNotificationSettingService;
      private readonly IBaseTransferService<IMessageTransferItem> _messageTransferService;
      private readonly IBaseTransferService<IFileTransferItem> _fileTransferService;
      private readonly IEmailService _emailService;
      private readonly ITokenService _tokenService;
      private readonly IMediator _mediator;

      public UserController(
          IUserService userService,
          IUserProfileService userProfileService,
          IUserPublicKeyPairService<UserX25519KeyPair> userX25519KeyPairService,
          IUserPublicKeyPairService<UserEd25519KeyPair> userEd25519KeyPairService,
          IUserSearchService userSearchService,
          IUserPrivacySettingService userPrivacySettingService,
          IUserEmailVerificationService userEmailVerificationService,
          IUserNotificationSettingService userNotificationSettingService,
          IBaseTransferService<IMessageTransferItem> messageService,
          IBaseTransferService<IFileTransferItem> fileService,
          IEmailService emailService,
          ITokenService tokenService,
          IMediator mediator
          )
      {
         _userService = userService;
         _userProfileService = userProfileService;
         _userX25519KeyPairService = userX25519KeyPairService;
         _userEd25519KeyPairService = userEd25519KeyPairService;
         _userSearchService = userSearchService;
         _userPrivacySettingService = userPrivacySettingService;
         _userEmailVerificationService = userEmailVerificationService;
         _userNotificationSettingService = userNotificationSettingService;
         _messageTransferService = messageService;
         _fileTransferService = fileService;
         _emailService = emailService;
         _tokenService = tokenService;
         _mediator = mediator;
      }

      [HttpPost("register")]
      public async Task<ActionResult<UserRegisterResponse>> RegisterAsync([FromBody] RegisterUserRequest request, CancellationToken cancellationToken)
      {
         var insertResult = await _mediator.Send(new InsertUserCommand(request.Username, request.Password, request.Email), cancellationToken);

         if (insertResult.Result != InsertUserResult.Success)
         {
            return new BadRequestObjectResult(new UserRegisterResponse(insertResult.Result));
         }

         if (insertResult.SendVerificationEmail)
         {
            BackgroundJob.Enqueue(() => _emailService.HangfireSendEmailVerificationAsync(insertResult.UserId));
         }

         return new OkObjectResult(new UserRegisterResponse(InsertUserResult.Success));
      }

      [Authorize]
      [HttpGet("sent/messages")]
      public async Task<IActionResult> GetSentMessagesAsync(CancellationToken cancellationToken)
      {
         var userId = _tokenService.ParseUserId(User);

         var sentMessagesSansRecipientInfo = (await _messageTransferService.FindBySenderAsync(userId, cancellationToken))
            .Select(x => new { x.Id, x.Subject, x.Recipient, x.Expiration })
            .OrderBy(x => x.Expiration);

         var sentMessages = new List<UserSentMessageDTO>();
         foreach (var item in sentMessagesSansRecipientInfo)
         {
            IUser? recipient = null;
            IUserProfile? recipientProfile = null;
            if (item.Recipient != Guid.Empty)
            {
               recipient = await _userService.ReadAsync(item.Recipient, cancellationToken);
               recipientProfile = await _userProfileService.ReadAsync(item.Recipient, cancellationToken);
            }
            sentMessages.Add(new UserSentMessageDTO(item.Id, item.Subject, item.Recipient, recipient?.Username, recipientProfile?.Alias, item.Expiration));
         }

         return new OkObjectResult(
            new UserSentMessagesResponse(sentMessages));
      }

      [Authorize]
      [HttpGet("sent/files")]
      public async Task<IActionResult> GetSentFilesAsync(CancellationToken cancellationToken)
      {
         var userId = _tokenService.ParseUserId(User);

         var sentFilesSansRecipientInfo = (await _fileTransferService.FindBySenderAsync(userId, cancellationToken))
            .Select(x => new { x.Id, x.FileName, x.Recipient, x.Expiration })
            .OrderBy(x => x.Expiration);

         var sentFiles = new List<UserSentFileDTO>();
         foreach (var item in sentFilesSansRecipientInfo)
         {
            IUser? recipient = null;
            IUserProfile? recipientProfile = null;
            if (item.Recipient != Guid.Empty)
            {
               recipient = await _userService.ReadAsync(item.Recipient, cancellationToken);
               recipientProfile = await _userProfileService.ReadAsync(item.Recipient, cancellationToken);
            }
            sentFiles.Add(new UserSentFileDTO(item.Id, item.FileName, item.Recipient, recipient?.Username, recipientProfile?.Alias, item.Expiration));
         }

         return new OkObjectResult(
            new UserSentFilesResponse(sentFiles));
      }

      [Authorize]
      [HttpGet("received/messages")]
      public async Task<IActionResult> GetReceivedMessagesAsync(CancellationToken cancellationToken)
      {
         var userId = _tokenService.ParseUserId(User);

         var receivedMessagesSansSenderInfo = (await _messageTransferService.FindByRecipientAsync(userId, cancellationToken))
            .Select(x => new { x.Id, x.Subject, x.Sender, x.Expiration })
            .OrderBy(x => x.Expiration);

         var receivedMessages = new List<UserReceivedMessageDTO>();
         foreach (var item in receivedMessagesSansSenderInfo)
         {
            IUser? sender = null;
            IUserProfile? senderProfile = null;
            if (item.Sender != Guid.Empty)
            {
               sender = await _userService.ReadAsync(item.Sender, cancellationToken);
               senderProfile = await _userProfileService.ReadAsync(item.Sender, cancellationToken);
            }
            receivedMessages.Add(new UserReceivedMessageDTO(item.Id, item.Subject, item.Sender, sender?.Username, senderProfile?.Alias, item.Expiration));
         }

         return new OkObjectResult(
            new UserReceivedMessagesResponse(receivedMessages));
      }

      [Authorize]
      [HttpGet("received/files")]
      public async Task<IActionResult> GetReceivedFilesAsync(CancellationToken cancellationToken)
      {
         var userId = _tokenService.ParseUserId(User);

         var receivedFilesSansSenderInfo = (await _fileTransferService.FindByRecipientAsync(userId, cancellationToken))
            .Select(x => new { x.Id, x.FileName, x.Sender, x.Expiration })
            .OrderBy(x => x.Expiration);

         var receivedFiles = new List<UserReceivedFileDTO>();
         foreach (var item in receivedFilesSansSenderInfo)
         {
            IUser? sender = null;
            IUserProfile? senderProfile = null;
            if (item.Sender != Guid.Empty)
            {
               sender = await _userService.ReadAsync(item.Sender, cancellationToken);
               senderProfile = await _userProfileService.ReadAsync(item.Sender, cancellationToken);
            }
            receivedFiles.Add(new UserReceivedFileDTO(item.Id, item.FileName, item.Sender, sender?.Username, senderProfile?.Alias, item.Expiration));
         }

         return new OkObjectResult(
            new UserReceivedFilesResponse(receivedFiles));
      }

      [Authorize]
      [HttpGet("settings")]
      public async Task<IActionResult> GetUserSettingsAsync(CancellationToken cancellationToken)
      {
         var userId = _tokenService.ParseUserId(User);
         var user = await _userService.ReadAsync(userId, cancellationToken);
         var userProfile = await _userProfileService.ReadAsync(userId, cancellationToken);
         var userPrivacy = await _userPrivacySettingService.ReadAsync(userId, cancellationToken);
         var userNotification = await _userNotificationSettingService.ReadAsync(userId, cancellationToken);

         return new OkObjectResult(
            new UserSettingsResponse(
             user.Username,
             user.Email,
             user.EmailVerified,
             userProfile?.Alias,
             userProfile?.About,
             userPrivacy?.Visibility ?? UserVisibilityLevel.None,
             userPrivacy?.AllowKeyExchangeRequests ?? false,
             userPrivacy?.ReceiveMessages ?? UserItemTransferPermission.None,
             userPrivacy?.ReceiveFiles ?? UserItemTransferPermission.None,
             userNotification?.EnableTransferNotifications ?? false,
             userNotification?.EmailNotifications ?? false,
             user.Created
         ));
      }

      [Authorize]
      [HttpPost("settings/profile")]
      public async Task<IActionResult> UpdateUserProfileAsync([FromBody] UpdateProfileRequest request, CancellationToken cancellationToken)
      {
         var userId = _tokenService.ParseUserId(User);
         var updateResult = await _userProfileService.UpsertAsync(userId, request.Alias, request.About, cancellationToken);
         var responseObject = new UpdateProfileResponse();

         if (updateResult)
         {
            return new OkObjectResult(responseObject);
         }
         else
         {
            return new BadRequestObjectResult(responseObject);
         }
      }

      [Authorize]
      [HttpPost("settings/contact")]
      public async Task<IActionResult> UpdateUserContactInfoAsync([FromBody] UpdateContactInfoRequest request, CancellationToken cancellationToken)
      {
         var userId = _tokenService.ParseUserId(User);

         if (ValidationService.IsPossibleEmailAddress(request.Email)
            && !ValidationService.IsValidEmailAddress(request.Email))
         {
            return new BadRequestObjectResult(
               new UpdateContactInfoResponse(UpdateContactInfoResult.EmailInvalid));
         }

         var user = await _userService.ReadAsync(userId, cancellationToken);
         if (ValidationService.IsPossibleEmailAddress(request.Email)
            && user.Email?.ToLower() != request.Email.ToLower()
            && !await _mediator.Send(new EmailAvailabilityQuery(request.Email), cancellationToken))
         {
            return new BadRequestObjectResult(
               new UpdateContactInfoResponse(UpdateContactInfoResult.EmailUnavailable));
         }

         var updateContactInfoResult = await _userService.UpdateContactInfoAsync(userId, request.Email, request.CurrentPassword, cancellationToken);

         if (updateContactInfoResult != UpdateContactInfoResult.Success)
         {
            var responseObject = new UpdateContactInfoResponse(updateContactInfoResult);
            return new BadRequestObjectResult(responseObject);
         }

         await _userNotificationSettingService.UpsertAsync(userId, false, false, default);
         await _userEmailVerificationService.DeleteAsync(userId, default);

         if (ValidationService.IsValidEmailAddress(request.Email))
         {
            BackgroundJob.Enqueue(() => _emailService.HangfireSendEmailVerificationAsync(userId));
         }

         var successResponse = new UpdateContactInfoResponse(UpdateContactInfoResult.Success);
         return new OkObjectResult(successResponse);
      }

      [Authorize]
      [HttpPost("settings/notification")]
      public async Task<IActionResult> UpdateUserNotificationPreferencesAsync([FromBody] UpdateNotificationSettingRequest request, CancellationToken cancellationToken)
      {
         var userId = _tokenService.ParseUserId(User);
         var user = await _userService.ReadAsync(userId, cancellationToken);

         if (request.EnableTransferNotifications
            && request.EmailNotifications
            && !user.EmailVerified)
         {
            return new BadRequestObjectResult(new UpdateNotificationSettingResponse(UpdateUserNotificationSettingResult.EmailAddressNotVerified));
         }

         await _userNotificationSettingService.UpsertAsync(userId, request.EnableTransferNotifications, request.EmailNotifications, cancellationToken);
         return new OkObjectResult(new UpdateNotificationSettingResponse(UpdateUserNotificationSettingResult.Success));
      }

      [Authorize]
      [HttpPost("settings/privacy")]
      public async Task<IActionResult> UpdateUserPrivacyAsync([FromBody] UpdatePrivacySettingRequest request, CancellationToken cancellationToken)
      {
         var userId = _tokenService.ParseUserId(User);
         var updateSuccess = await _userPrivacySettingService.UpsertAsync(userId, request.AllowKeyExchangeRequests, request.VisibilityLevel, request.FileTransferPermission, request.MessageTransferPermission, cancellationToken);

         if (updateSuccess)
         {
            return new OkObjectResult(new UpdatePrivacySettingResponse());
         }
         else
         {
            return new BadRequestObjectResult(new UpdatePrivacySettingResponse());
         }
      }

      [Authorize]
      [HttpPost("settings/keys/x25519")]
      public async Task<IActionResult> UpdateDiffieHellmanKeysAsync([FromBody] UpdateKeysRequest body, CancellationToken cancellationToken)
      {
         var userId = _tokenService.ParseUserId(User);

         var insertResult = await _userX25519KeyPairService.InsertUserPublicKeyPairAsync(userId, body.EncryptedPrivateKeyBase64, body.PublicKeyBase64, body.ClientIVBase64, cancellationToken);
         if (insertResult)
         {
            return new OkObjectResult(
                new UpdateKeysResponse());
         }
         else
         {
            return new BadRequestObjectResult(
                new UpdateKeysResponse());
         }
      }

      [Authorize]
      [HttpPost("settings/keys/ed25519")]
      public async Task<IActionResult> UpdateDigitalSignatureKeysAsync([FromBody] UpdateKeysRequest body, CancellationToken cancellationToken)
      {
         var userId = _tokenService.ParseUserId(User);

         var insertResult = await _userEd25519KeyPairService.InsertUserPublicKeyPairAsync(userId, body.EncryptedPrivateKeyBase64, body.PublicKeyBase64, body.ClientIVBase64, cancellationToken);
         if (insertResult)
         {
            return new OkObjectResult(
                new UpdateKeysResponse());
         }
         else
         {
            return new BadRequestObjectResult(
                new UpdateKeysResponse());
         }
      }

      [Authorize]
      [HttpGet("search/username")]
      public async Task<IActionResult> SearchByUsernameAsync([FromQuery] string value, [FromQuery] int index, [FromQuery] int count, CancellationToken cancellationToken)
      {
         var searchPartyId = _tokenService.ParseUserId(User);
         var (total, users) = await _userSearchService.SearchByUsernameAsync(searchPartyId, value, index, count, cancellationToken);
         var dtoUsers = users
             .Select(x => new UserSearchResultDTO(x.Id, x.Username, x.Alias))
             .ToList();

         return new OkObjectResult(
             new UserSearchResponse(total, dtoUsers));
      }

      [Authorize]
      [HttpGet("search/alias")]
      public async Task<IActionResult> SearchByAliasAsync([FromQuery] string value, [FromQuery] int index, [FromQuery] int count, CancellationToken cancellationToken)
      {
         var searchPartyId = _tokenService.ParseUserId(User);
         var (total, users) = await _userSearchService.SearchByAliasAsync(searchPartyId, value, index, count, cancellationToken);
         var dtoUsers = users
             .Select(x => new UserSearchResultDTO(x.Id, x.Username, x.Alias))
             .ToList();

         return new OkObjectResult(
             new UserSearchResponse(total, dtoUsers));
      }

      [HttpGet("{username}")]
      public async Task<IActionResult> GetPublicUserProfileAsync(string username, CancellationToken cancellationToken)
      {
         var visitorId = _tokenService.ParseUserId(User);
         var notFoundResponse = new UserPublicProfileResponse(default, null, null, null, false, false, false, false, null, null);

         var user = await _userService.ReadAsync(username, cancellationToken);
         if (user is null)
         {
            return new NotFoundObjectResult(notFoundResponse);
         }

         var userProfile = await _userProfileService.ReadAsync(user.Id, cancellationToken);
         if (userProfile is null)
         {
            return new NotFoundObjectResult(notFoundResponse);
         }

         var userPrivacy = await _userPrivacySettingService.ReadAsync(user.Id, cancellationToken);
         if (userPrivacy is null)
         {
            return new NotFoundObjectResult(notFoundResponse);
         }

         var userAllowsRequestorToViewProfile = await _userPrivacySettingService.IsUserViewableByPartyAsync(user.Id, visitorId, cancellationToken);
         if (userAllowsRequestorToViewProfile)
         {
            var userPublicDHKey = await _userX25519KeyPairService.GetUserPublicKeyAsync(user.Id, cancellationToken);
            var userPublicDSAKey = await _userEd25519KeyPairService.GetUserPublicKeyAsync(user.Id, cancellationToken);

            var visitorCanSendMessages = await _userPrivacySettingService.DoesUserAcceptMessagesFromOtherPartyAsync(user.Id, visitorId, cancellationToken);
            var visitorCanSendFiles = await _userPrivacySettingService.DoesUserAcceptFilesFromOtherPartyAsync(user.Id, visitorId, cancellationToken);

            return new OkObjectResult(
               new UserPublicProfileResponse(user.Id, user.Username, userProfile.Alias, userProfile.About, userPrivacy.AllowKeyExchangeRequests,
               true, visitorCanSendMessages, visitorCanSendFiles, userPublicDHKey, userPublicDSAKey));
         }
         else
         {
            return new NotFoundObjectResult(notFoundResponse);
         }
      }

      [HttpPost("verify")]
      public async Task<IActionResult> VerifyUserEmailAddressAsync([FromBody] VerifyUserEmailAddressRequest request, CancellationToken cancellationToken)
      {
         var verificationCode = EmailVerificationEncoder.DecodeVerificationCodeFromUrlSafe(request.Code);

         var emailVerificationEntity = await _userEmailVerificationService.ReadCodeAsync(verificationCode, cancellationToken);
         if (emailVerificationEntity is null)
         {
            return new NotFoundObjectResult(
               new UserEmailVerificationResponse(false));
         }

         var signature = EmailVerificationEncoder.DecodeSignatureFromUrlSafe(request.Signature);

         var verificationKeyPem = Encoding.UTF8.GetString(emailVerificationEntity.VerificationKey);
         var verificationKey = KeyConversion.ConvertEd25519PublicKeyFromPEM(verificationKeyPem);

         var verifier = new ECDSA();
         verifier.InitializeVerifier(verificationKey);
         verifier.VerifierDigestChunk(verificationCode.ToByteArray());
         if (!verifier.VerifySignature(signature))
         {
            return new NotFoundObjectResult(
               new UserEmailVerificationResponse(false));
         }

         await _userService.UpdateEmailAddressVerification(emailVerificationEntity.Owner, true, default);
         await _userEmailVerificationService.DeleteAsync(emailVerificationEntity.Owner, default);

         return new OkObjectResult(
            new UserEmailVerificationResponse(true));
      }
   }
}

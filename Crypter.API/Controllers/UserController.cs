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

using Crypter.API.Methods;
using Crypter.API.Services;
using Crypter.Common.Services;
using Crypter.Contracts.Common;
using Crypter.Contracts.Common.Enum;
using Crypter.Contracts.Features.User.GetPublicProfile;
using Crypter.Contracts.Features.User.GetReceivedTransfers;
using Crypter.Contracts.Features.User.GetSentTransfers;
using Crypter.Contracts.Features.User.GetSettings;
using Crypter.Contracts.Features.User.Register;
using Crypter.Contracts.Features.User.Search;
using Crypter.Contracts.Features.User.UpdateContactInfo;
using Crypter.Contracts.Features.User.UpdateKeys;
using Crypter.Contracts.Features.User.UpdateNotificationSettings;
using Crypter.Contracts.Features.User.UpdatePrivacySettings;
using Crypter.Contracts.Features.User.UpdateProfile;
using Crypter.Contracts.Features.User.VerifyEmailAddress;
using Crypter.Core.Features.User.Commands;
using Crypter.Core.Features.User.Queries;
using Crypter.Core.Interfaces;
using Crypter.Core.Models;
using Crypter.CryptoLib;
using Crypter.CryptoLib.Crypto;
using Hangfire;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserRegisterResponse))]
      [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
      public async Task<IActionResult> RegisterAsync([FromBody] UserRegisterRequest request, CancellationToken cancellationToken)
      {
         var insertResult = await _mediator.Send(new InsertUserCommand(request.Username, request.Password, request.Email), cancellationToken);

         insertResult.DoRight(x =>
         {
            if (x.SendVerificationEmail)
            {
               BackgroundJob.Enqueue(() => _emailService.HangfireSendEmailVerificationAsync(x.UserId));
            }
         });

         return insertResult.Match<IActionResult>(
            left => new BadRequestObjectResult(new ErrorResponse(left)),
            right => new OkObjectResult(new UserRegisterResponse()));
      }

      [Authorize]
      [HttpGet("sent/messages")]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserSentMessagesResponse))]
      [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(void))]
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

         return new OkObjectResult(new UserSentMessagesResponse(sentMessages));
      }

      [Authorize]
      [HttpGet("sent/files")]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserSentFilesResponse))]
      [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(void))]
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

         return new OkObjectResult(new UserSentFilesResponse(sentFiles));
      }

      [Authorize]
      [HttpGet("received/messages")]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserReceivedMessagesResponse))]
      [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(void))]
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

         return new OkObjectResult(new UserReceivedMessagesResponse(receivedMessages));
      }

      [Authorize]
      [HttpGet("received/files")]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserReceivedFilesResponse))]
      [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(void))]
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

         return new OkObjectResult(new UserReceivedFilesResponse(receivedFiles));
      }

      [Authorize]
      [HttpGet("settings")]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserSettingsResponse))]
      [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(void))]
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
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UpdateProfileResponse))]
      [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(void))]
      [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
      public async Task<IActionResult> UpdateUserProfileAsync([FromBody] UpdateProfileRequest request, CancellationToken cancellationToken)
      {
         var userId = _tokenService.ParseUserId(User);
         var updateSuccess = await _userProfileService.UpsertAsync(userId, request.Alias, request.About, cancellationToken);

         return updateSuccess
            ? new OkObjectResult(new UpdateProfileResponse())
            : new BadRequestObjectResult(new ErrorResponse(UpdateProfileError.UnknownError));
      }

      [Authorize]
      [HttpPost("settings/contact")]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UpdateContactInfoResponse))]
      [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(void))]
      [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
      public async Task<IActionResult> UpdateUserContactInfoAsync([FromBody] UpdateContactInfoRequest request, CancellationToken cancellationToken)
      {
         var userId = _tokenService.ParseUserId(User);

         if (ValidationService.IsPossibleEmailAddress(request.Email)
            && !ValidationService.IsValidEmailAddress(request.Email))
         {
            return new BadRequestObjectResult(new ErrorResponse(UpdateContactInfoError.EmailInvalid));
         }

         var user = await _userService.ReadAsync(userId, cancellationToken);
         if (ValidationService.IsPossibleEmailAddress(request.Email)
            && user.Email?.ToLower() != request.Email.ToLower()
            && !await _mediator.Send(new EmailAvailabilityQuery(request.Email), cancellationToken))
         {
            return new BadRequestObjectResult(new ErrorResponse(UpdateContactInfoError.EmailUnavailable));
         }

         var (UpdateSuccess, UpdateError) = await _userService.UpdateContactInfoAsync(userId, request.Email, request.CurrentPassword, cancellationToken);

         if (!UpdateSuccess)
         {
            return new BadRequestObjectResult(new ErrorResponse(UpdateError));
         }

         await _userNotificationSettingService.UpsertAsync(userId, false, false, default);
         await _userEmailVerificationService.DeleteAsync(userId, default);

         if (ValidationService.IsValidEmailAddress(request.Email))
         {
            BackgroundJob.Enqueue(() => _emailService.HangfireSendEmailVerificationAsync(userId));
         }

         return new OkObjectResult(new UpdateContactInfoResponse());
      }

      [Authorize]
      [HttpPost("settings/notification")]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UpdateNotificationSettingsResponse))]
      [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(void))]
      [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
      public async Task<IActionResult> UpdateUserNotificationPreferencesAsync([FromBody] UpdateNotificationSettingsRequest request, CancellationToken cancellationToken)
      {
         var userId = _tokenService.ParseUserId(User);
         var user = await _userService.ReadAsync(userId, cancellationToken);

         if (request.EnableTransferNotifications
            && request.EmailNotifications
            && !user.EmailVerified)
         {
            return new BadRequestObjectResult(new ErrorResponse(UpdateNotificationSettingsError.EmailAddressNotVerified));
         }

         await _userNotificationSettingService.UpsertAsync(userId, request.EnableTransferNotifications, request.EmailNotifications, cancellationToken);
         return new OkObjectResult(new UpdateNotificationSettingsResponse());
      }

      [Authorize]
      [HttpPost("settings/privacy")]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UpdatePrivacySettingsResponse))]
      [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(void))]
      [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
      public async Task<IActionResult> UpdateUserPrivacyAsync([FromBody] UpdatePrivacySettingsRequest request, CancellationToken cancellationToken)
      {
         var userId = _tokenService.ParseUserId(User);
         var updateSuccess = await _userPrivacySettingService.UpsertAsync(userId, request.AllowKeyExchangeRequests, request.VisibilityLevel, request.FileTransferPermission, request.MessageTransferPermission, cancellationToken);

         if (updateSuccess)
         {
            return new OkObjectResult(new UpdatePrivacySettingsResponse());
         }
         else
         {
            return new BadRequestObjectResult(new ErrorResponse(UpdatePrivacySettingsError.UnknownError));
         }
      }

      [Authorize]
      [HttpPost("settings/keys/x25519")]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UpdateKeysResponse))]
      [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(void))]
      [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
      public async Task<IActionResult> UpdateDiffieHellmanKeysAsync([FromBody] UpdateKeysRequest body, CancellationToken cancellationToken)
      {
         var userId = _tokenService.ParseUserId(User);

         var insertSuccess = await _userX25519KeyPairService.InsertUserPublicKeyPairAsync(userId, body.EncryptedPrivateKeyBase64, body.PublicKeyBase64, body.ClientIVBase64, cancellationToken);
         if (insertSuccess)
         {
            return new OkObjectResult(new UpdateKeysResponse());
         }
         else
         {
            return new BadRequestObjectResult(new ErrorResponse(UpdateKeysError.UnknownError));
         }
      }

      [Authorize]
      [HttpPost("settings/keys/ed25519")]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UpdateKeysResponse))]
      [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(void))]
      [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ErrorResponse))]
      public async Task<IActionResult> UpdateDigitalSignatureKeysAsync([FromBody] UpdateKeysRequest body, CancellationToken cancellationToken)
      {
         var userId = _tokenService.ParseUserId(User);

         var insertSuccess = await _userEd25519KeyPairService.InsertUserPublicKeyPairAsync(userId, body.EncryptedPrivateKeyBase64, body.PublicKeyBase64, body.ClientIVBase64, cancellationToken);
         if (insertSuccess)
         {
            return new OkObjectResult(new UpdateKeysResponse());
         }
         else
         {
            return new BadRequestObjectResult(new ErrorResponse(UpdateKeysError.UnknownError));
         }
      }

      [Authorize]
      [HttpGet("search/username")]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserSearchResponse))]
      [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(void))]
      public async Task<IActionResult> SearchByUsernameAsync([FromQuery] string value, [FromQuery] int index, [FromQuery] int count, CancellationToken cancellationToken)
      {
         var requestorId = _tokenService.ParseUserId(User);
         var result = await _mediator.Send(new UserSearchQuery(requestorId, value, UserSearchKeywordField.Username, index, count), cancellationToken);
         return new OkObjectResult(new UserSearchResponse(result.Total, result.Users));
      }

      [Authorize]
      [HttpGet("search/alias")]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserSearchResponse))]
      [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(void))]
      public async Task<IActionResult> SearchByAliasAsync([FromQuery] string value, [FromQuery] int index, [FromQuery] int count, CancellationToken cancellationToken)
      {
         var requestorId = _tokenService.ParseUserId(User);
         var result = await _mediator.Send(new UserSearchQuery(requestorId, value, UserSearchKeywordField.Alias, index, count), cancellationToken);
         return new OkObjectResult(new UserSearchResponse(result.Total, result.Users));
      }

      [HttpGet("{username}")]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(GetUserPublicProfileResponse))]
      [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(void))]
      [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
      public async Task<IActionResult> GetPublicUserProfileAsync(string username, CancellationToken cancellationToken)
      {
         var visitorId = _tokenService.ParseUserId(User);

         var user = await _userService.ReadAsync(username, cancellationToken);
         if (user is null)
         {
            return new NotFoundObjectResult(new ErrorResponse(GetUserPublicProfileError.NotFound));
         }

         var userProfile = await _userProfileService.ReadAsync(user.Id, cancellationToken);
         if (userProfile is null)
         {
            return new NotFoundObjectResult(new ErrorResponse(GetUserPublicProfileError.NotFound));
         }

         var userPrivacy = await _userPrivacySettingService.ReadAsync(user.Id, cancellationToken);
         if (userPrivacy is null)
         {
            return new NotFoundObjectResult(new ErrorResponse(GetUserPublicProfileError.NotFound));
         }

         var userAllowsRequestorToViewProfile = await _userPrivacySettingService.IsUserViewableByPartyAsync(user.Id, visitorId, cancellationToken);
         if (userAllowsRequestorToViewProfile)
         {
            var userPublicDHKey = await _userX25519KeyPairService.GetUserPublicKeyAsync(user.Id, cancellationToken);
            var userPublicDSAKey = await _userEd25519KeyPairService.GetUserPublicKeyAsync(user.Id, cancellationToken);

            var visitorCanSendMessages = await _userPrivacySettingService.DoesUserAcceptMessagesFromOtherPartyAsync(user.Id, visitorId, cancellationToken);
            var visitorCanSendFiles = await _userPrivacySettingService.DoesUserAcceptFilesFromOtherPartyAsync(user.Id, visitorId, cancellationToken);

            return new OkObjectResult(
               new GetUserPublicProfileResponse(user.Id, user.Username, userProfile.Alias, userProfile.About, userPrivacy.AllowKeyExchangeRequests,
               true, visitorCanSendMessages, visitorCanSendFiles, userPublicDHKey, userPublicDSAKey));
         }
         else
         {
            return new NotFoundObjectResult(new ErrorResponse(GetUserPublicProfileError.NotFound));
         }
      }

      [HttpPost("verify")]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(VerifyEmailAddressResponse))]
      [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(void))]
      [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ErrorResponse))]
      public async Task<IActionResult> VerifyUserEmailAddressAsync([FromBody] VerifyEmailAddressRequest request, CancellationToken cancellationToken)
      {
         var verificationCode = EmailVerificationEncoder.DecodeVerificationCodeFromUrlSafe(request.Code);

         var emailVerificationEntity = await _userEmailVerificationService.ReadCodeAsync(verificationCode, cancellationToken);
         if (emailVerificationEntity is null)
         {
            return new NotFoundObjectResult(new ErrorResponse(VerifyEmailAddressError.NotFound));
         }

         var signature = EmailVerificationEncoder.DecodeSignatureFromUrlSafe(request.Signature);

         var verificationKeyPem = Encoding.UTF8.GetString(emailVerificationEntity.VerificationKey);
         var verificationKey = KeyConversion.ConvertEd25519PublicKeyFromPEM(verificationKeyPem);

         var verifier = new ECDSA();
         verifier.InitializeVerifier(verificationKey);
         verifier.VerifierDigestChunk(verificationCode.ToByteArray());
         if (!verifier.VerifySignature(signature))
         {
            return new NotFoundObjectResult(new ErrorResponse(VerifyEmailAddressError.NotFound));
         }

         await _userService.UpdateEmailAddressVerification(emailVerificationEntity.Owner, true, default);
         await _userEmailVerificationService.DeleteAsync(emailVerificationEntity.Owner, default);

         return new OkObjectResult(new VerifyEmailAddressResponse());
      }
   }
}

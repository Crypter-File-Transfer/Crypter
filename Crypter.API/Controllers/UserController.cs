using Crypter.API.Controllers.Methods;
using Crypter.API.Services;
using Crypter.Common.Services;
using Crypter.Contracts.DTO;
using Crypter.Contracts.Enum;
using Crypter.Contracts.Requests;
using Crypter.Contracts.Responses;
using Crypter.Core.Interfaces;
using Crypter.Core.Models;
using Crypter.CryptoLib;
using Crypter.CryptoLib.Crypto;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Crypter.API.Controllers
{
   [ApiController]
   [Route("api/user")]
   public class UserController : ControllerBase
   {
      private readonly IUserService UserService;
      private readonly IUserProfileService UserProfileService;
      private readonly IUserPublicKeyPairService<UserX25519KeyPair> UserDiffieHellmanKeyPairService;
      private readonly IUserPublicKeyPairService<UserEd25519KeyPair> UserDigitalSignatureKeyPairService;
      private readonly IUserSearchService UserSearchService;
      private readonly IUserPrivacySettingService UserPrivacySettingService;
      private readonly IUserEmailVerificationService UserEmailVerificationService;
      private readonly IUserNotificationSettingService UserNotificationSettingService;
      private readonly IBaseTransferService<MessageTransfer> MessageService;
      private readonly IBaseTransferService<FileTransfer> FileService;
      private readonly IEmailService EmailService;
      private readonly byte[] TokenSecretKey;

      public UserController(
          IUserService userService,
          IUserProfileService userProfileService,
          IUserPublicKeyPairService<UserX25519KeyPair> userDiffieHellmanKeyPairService,
          IUserPublicKeyPairService<UserEd25519KeyPair> userDigitalSignatureKeyPairService,
          IUserSearchService userSearchService,
          IUserPrivacySettingService userPrivacySettingService,
          IUserEmailVerificationService userEmailVerificationService,
          IUserNotificationSettingService userNotificationSettingService,
          IBaseTransferService<MessageTransfer> messageService,
          IBaseTransferService<FileTransfer> fileService,
          IEmailService emailService,
          IConfiguration configuration
          )
      {
         UserService = userService;
         UserProfileService = userProfileService;
         UserDiffieHellmanKeyPairService = userDiffieHellmanKeyPairService;
         UserDigitalSignatureKeyPairService = userDigitalSignatureKeyPairService;
         UserSearchService = userSearchService;
         UserPrivacySettingService = userPrivacySettingService;
         UserEmailVerificationService = userEmailVerificationService;
         UserNotificationSettingService = userNotificationSettingService;
         MessageService = messageService;
         FileService = fileService;
         EmailService = emailService;
         TokenSecretKey = Encoding.UTF8.GetBytes(configuration["Secrets:TokenSigningKey"]);
      }

      [HttpPost("register")]
      public async Task<IActionResult> RegisterAsync([FromBody] RegisterUserRequest request)
      {
         if (!ValidationService.IsValidPassword(request.Password))
         {
            return new BadRequestObjectResult(
                new UserRegisterResponse(InsertUserResult.PasswordRequirementsNotMet));
         }

         if (ValidationService.IsPossibleEmailAddress(request.Email)
            && !ValidationService.IsValidEmailAddress(request.Email))
         {
            return new BadRequestObjectResult(
               new UserRegisterResponse(InsertUserResult.InvalidEmailAddress));
         }

         (var insertResult, var userId) = await UserService.InsertAsync(request.Username, request.Password, request.Email);
         var responseObject = new UserRegisterResponse(insertResult);

         if (insertResult == InsertUserResult.Success)
         {
            if (ValidationService.IsPossibleEmailAddress(request.Email))
            {
               BackgroundJob.Enqueue(() => EmailService.HangfireSendEmailVerificationAsync(userId));
            }
            return new OkObjectResult(responseObject);
         }
         else
         {
            return new BadRequestObjectResult(responseObject);
         }
      }

      [HttpPost("authenticate")]
      public async Task<IActionResult> AuthenticateAsync([FromBody] AuthenticateUserRequest request)
      {
         var user = await UserService.AuthenticateAsync(request.Username, request.Password);
         if (user == null)
         {
            return new NotFoundObjectResult(
               new UserAuthenticateResponse(default, null));
         }

         var tokenHandler = new JwtSecurityTokenHandler();
         var tokenDescriptor = new SecurityTokenDescriptor
         {
            Subject = new ClaimsIdentity(new Claim[]
            {
               new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
            }),
            Audience = "crypter.dev",
            Issuer = "crypter.dev/api",
            Expires = DateTime.UtcNow.AddDays(1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(TokenSecretKey), SecurityAlgorithms.HmacSha256Signature)
         };
         var token = tokenHandler.CreateToken(tokenDescriptor);
         var tokenString = tokenHandler.WriteToken(token);

         var userDHKeyPair = await UserDiffieHellmanKeyPairService.GetUserPublicKeyPairAsync(user.Id);
         var userDSAKeyPair = await UserDigitalSignatureKeyPairService.GetUserPublicKeyPairAsync(user.Id);

         BackgroundJob.Enqueue(() => UserService.UpdateLastLoginTime(user.Id, DateTime.UtcNow));

         return new OkObjectResult(
             new UserAuthenticateResponse(user.Id, tokenString, userDHKeyPair?.PrivateKey, userDSAKeyPair?.PrivateKey)
         );
      }

      [Authorize]
      [HttpGet("authenticate/refresh")]
      public IActionResult RefreshAuthenticationAsync()
      {
         var userId = ClaimsParser.ParseUserId(User);

         var tokenHandler = new JwtSecurityTokenHandler();
         var tokenDescriptor = new SecurityTokenDescriptor
         {
            Subject = new ClaimsIdentity(new Claim[]
            {
               new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            }),
            Audience = "crypter.dev",
            Issuer = "crypter.dev/api",
            Expires = DateTime.UtcNow.AddDays(1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(TokenSecretKey), SecurityAlgorithms.HmacSha256Signature)
         };
         var token = tokenHandler.CreateToken(tokenDescriptor);
         var tokenString = tokenHandler.WriteToken(token);

         BackgroundJob.Enqueue(() => UserService.UpdateLastLoginTime(userId, DateTime.UtcNow));

         return new OkObjectResult(
            new UserAuthenticationRefreshResponse(tokenString));
      }

      [Authorize]
      [HttpGet("sent/messages")]
      public async Task<IActionResult> GetSentMessagesAsync()
      {
         var userId = ClaimsParser.ParseUserId(User);

         var sentMessagesSansRecipientInfo = (await MessageService.FindBySenderAsync(userId))
            .Select(x => new { x.Id, x.Subject, x.Recipient, x.Expiration })
            .OrderBy(x => x.Expiration);

         var sentMessages = new List<UserSentMessageDTO>();
         foreach (var item in sentMessagesSansRecipientInfo)
         {
            IUser recipient = null;
            IUserProfile recipientProfile = null;
            if (item.Recipient != Guid.Empty)
            {
               recipient = await UserService.ReadAsync(item.Recipient);
               recipientProfile = await UserProfileService.ReadAsync(item.Recipient);
            }
            sentMessages.Add(new UserSentMessageDTO(item.Id, item.Subject, item.Recipient, recipient?.Username, recipientProfile?.Alias, item.Expiration));
         }

         return new OkObjectResult(
            new UserSentMessagesResponse(sentMessages));
      }

      [Authorize]
      [HttpGet("sent/files")]
      public async Task<IActionResult> GetSentFilesAsync()
      {
         var userId = ClaimsParser.ParseUserId(User);

         var sentFilesSansRecipientInfo = (await FileService.FindBySenderAsync(userId))
            .Select(x => new { x.Id, x.FileName, x.Recipient, x.Expiration })
            .OrderBy(x => x.Expiration);

         var sentFiles = new List<UserSentFileDTO>();
         foreach (var item in sentFilesSansRecipientInfo)
         {
            IUser recipient = null;
            IUserProfile recipientProfile = null;
            if (item.Recipient != Guid.Empty)
            {
               recipient = await UserService.ReadAsync(item.Recipient);
               recipientProfile = await UserProfileService.ReadAsync(item.Recipient);
            }
            sentFiles.Add(new UserSentFileDTO(item.Id, item.FileName, item.Recipient, recipient?.Username, recipientProfile?.Alias, item.Expiration));
         }

         return new OkObjectResult(
            new UserSentFilesResponse(sentFiles));
      }

      [Authorize]
      [HttpGet("received/messages")]
      public async Task<IActionResult> GetReceivedMessagesAsync()
      {
         var userId = ClaimsParser.ParseUserId(User);

         var receivedMessagesSansSenderInfo = (await MessageService.FindByRecipientAsync(userId))
            .Select(x => new { x.Id, x.Subject, x.Sender, x.Expiration })
            .OrderBy(x => x.Expiration);

         var receivedMessages = new List<UserReceivedMessageDTO>();
         foreach (var item in receivedMessagesSansSenderInfo)
         {
            IUser sender = null;
            IUserProfile senderProfile = null;
            if (item.Sender != Guid.Empty)
            {
               sender = await UserService.ReadAsync(item.Sender);
               senderProfile = await UserProfileService.ReadAsync(item.Sender);
            }
            receivedMessages.Add(new UserReceivedMessageDTO(item.Id, item.Subject, item.Sender, sender?.Username, senderProfile?.Alias, item.Expiration));
         }

         return new OkObjectResult(
            new UserReceivedMessagesResponse(receivedMessages));
      }

      [Authorize]
      [HttpGet("received/files")]
      public async Task<IActionResult> GetReceivedFilesAsync()
      {
         var userId = ClaimsParser.ParseUserId(User);

         var receivedFilesSansSenderInfo = (await FileService.FindByRecipientAsync(userId))
            .Select(x => new { x.Id, x.FileName, x.Sender, x.Expiration })
            .OrderBy(x => x.Expiration);

         var receivedFiles = new List<UserReceivedFileDTO>();
         foreach (var item in receivedFilesSansSenderInfo)
         {
            IUser sender = null;
            IUserProfile senderProfile = null;
            if (item.Sender != Guid.Empty)
            {
               sender = await UserService.ReadAsync(item.Sender);
               senderProfile = await UserProfileService.ReadAsync(item.Sender);
            }
            receivedFiles.Add(new UserReceivedFileDTO(item.Id, item.FileName, item.Sender, sender?.Username, senderProfile?.Alias, item.Expiration));
         }

         return new OkObjectResult(
            new UserReceivedFilesResponse(receivedFiles));
      }

      [Authorize]
      [HttpGet("settings")]
      public async Task<IActionResult> GetUserSettingsAsync()
      {
         var userId = ClaimsParser.ParseUserId(User);
         var user = await UserService.ReadAsync(userId);
         var userProfile = await UserProfileService.ReadAsync(userId);
         var userPrivacy = await UserPrivacySettingService.ReadAsync(userId);
         var userNotification = await UserNotificationSettingService.ReadAsync(userId);

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
      public async Task<IActionResult> UpdateUserProfileAsync([FromBody] UpdateProfileRequest request)
      {
         var userId = ClaimsParser.ParseUserId(User);
         var updateResult = await UserProfileService.UpsertAsync(userId, request.Alias, request.About);
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
      public async Task<IActionResult> UpdateUserContactInfoAsync([FromBody] UpdateContactInfoRequest request)
      {
         var userId = ClaimsParser.ParseUserId(User);
         var updateContactInfoResult = await UserService.UpdateContactInfoAsync(userId, request.Email, request.CurrentPassword);

         if (updateContactInfoResult != UpdateContactInfoResult.Success)
         {
            var responseObject = new UpdateContactInfoResponse(updateContactInfoResult);
            return new BadRequestObjectResult(responseObject);
         }

         var resetNotificationSettingResult = await UserNotificationSettingService.UpsertAsync(userId, false, false);
         if (!resetNotificationSettingResult)
         {
            var responseObject = new UpdateContactInfoResponse(UpdateContactInfoResult.ErrorResettingNotificationPreferences);
            return new BadRequestObjectResult(responseObject);
         }

         var successResponse = new UpdateContactInfoResponse(UpdateContactInfoResult.Success);
         return new OkObjectResult(successResponse);
      }

      [Authorize]
      [HttpPost("settings/notification")]
      public async Task<IActionResult> UpdateUserNotificationPreferencesAsync([FromBody] UpdateNotificationSettingRequest request)
      {
         var userId = ClaimsParser.ParseUserId(User);
         var user = await UserService.ReadAsync(userId);

         if (request.EnableTransferNotifications 
            && request.EmailNotifications
            && !user.EmailVerified)
         {
            return new BadRequestObjectResult(new UpdateNotificationSettingResponse(UpdateUserNotificationSettingResult.EmailAddressNotVerified));
         }

         var updateResult = await UserNotificationSettingService.UpsertAsync(userId, request.EnableTransferNotifications, request.EmailNotifications);
         if (updateResult)
         {
            return new OkObjectResult(new UpdateNotificationSettingResponse(UpdateUserNotificationSettingResult.Success));
         }
         else
         {
            return new BadRequestObjectResult(new UpdateNotificationSettingResponse(UpdateUserNotificationSettingResult.UnknownFailure));
         }
      }

      [Authorize]
      [HttpPost("settings/privacy")]
      public async Task<IActionResult> UpdateUserPrivacyAsync([FromBody] UpdatePrivacySettingRequest request)
      {
         var userId = ClaimsParser.ParseUserId(User);
         var updateSuccess = await UserPrivacySettingService.UpsertAsync(userId, request.AllowKeyExchangeRequests, request.VisibilityLevel, request.FileTransferPermission, request.MessageTransferPermission);

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
      public async Task<IActionResult> UpdateDiffieHellmanKeysAsync([FromBody] UpdateKeysRequest body)
      {
         var userId = ClaimsParser.ParseUserId(User);

         var insertResult = await UserDiffieHellmanKeyPairService.InsertUserPublicKeyPairAsync(userId, body.EncryptedPrivateKeyBase64, body.PublicKey);
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
      public async Task<IActionResult> UpdateDigitalSignatureKeysAsync([FromBody] UpdateKeysRequest body)
      {
         var userId = ClaimsParser.ParseUserId(User);

         var insertResult = await UserDigitalSignatureKeyPairService.InsertUserPublicKeyPairAsync(userId, body.EncryptedPrivateKeyBase64, body.PublicKey);
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
      public async Task<IActionResult> SearchByUsernameAsync([FromQuery] string value, [FromQuery] int index, [FromQuery] int count)
      {
         var searchPartyId = ClaimsParser.ParseUserId(User);
         var (total, users) = await UserSearchService.SearchByUsernameAsync(searchPartyId, value, index, count);
         var dtoUsers = users
             .Select(x => new UserSearchResultDTO(x.Id, x.Username, x.Alias))
             .ToList();

         return new OkObjectResult(
             new UserSearchResponse(total, dtoUsers));
      }

      [Authorize]
      [HttpGet("search/alias")]
      public async Task<IActionResult> SearchByAliasAsync([FromQuery] string value, [FromQuery] int index, [FromQuery] int count)
      {
         var searchPartyId = ClaimsParser.ParseUserId(User);
         var (total, users) = await UserSearchService.SearchByAliasAsync(searchPartyId, value, index, count);
         var dtoUsers = users
             .Select(x => new UserSearchResultDTO(x.Id, x.Username, x.Alias))
             .ToList();

         return new OkObjectResult(
             new UserSearchResponse(total, dtoUsers));
      }

      [HttpGet("{username}")]
      public async Task<IActionResult> GetPublicUserProfileAsync(string username)
      {
         var visitorId = ClaimsParser.ParseUserId(User);
         var notFoundResponse = new UserPublicProfileResponse(default, null, null, null, false, false, false, false, null, null);

         var requestor = ClaimsParser.ParseUserId(User);
         var user = await UserService.ReadAsync(username);
         if (user is null)
         {
            return new NotFoundObjectResult(notFoundResponse);
         }

         var userProfile = await UserProfileService.ReadAsync(user.Id);
         if (userProfile is null)
         {
            return new NotFoundObjectResult(notFoundResponse);
         }

         var userPrivacy = await UserPrivacySettingService.ReadAsync(user.Id);
         if (userPrivacy is null)
         {
            return new NotFoundObjectResult(notFoundResponse);
         }

         var userAllowsRequestorToViewProfile = await UserPrivacySettingService.IsUserViewableByPartyAsync(user.Id, requestor);
         if (userAllowsRequestorToViewProfile)
         {
            var userPublicDHKey = await UserDiffieHellmanKeyPairService.GetUserPublicKeyAsync(user.Id);
            var userPublicDSAKey = await UserDigitalSignatureKeyPairService.GetUserPublicKeyAsync(user.Id);

            var visitorCanSendMessages = await UserPrivacySettingService.DoesUserAcceptMessagesFromOtherPartyAsync(user.Id, visitorId);
            var visitorCanSendFiles = await UserPrivacySettingService.DoesUserAcceptFilesFromOtherPartyAsync(user.Id, visitorId);

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
      public async Task<IActionResult> VerifyUserEmailAddressAsync([FromBody] VerifyUserEmailAddressRequest request)
      {
         var verificationCode = EmailVerificationEncoder.DecodeVerificationCodeFromUrlSafe(request.Code);

         var emailVerificationEntity = await UserEmailVerificationService.ReadCodeAsync(verificationCode);
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

         await UserService.UpdateEmailAddressVerification(emailVerificationEntity.Owner, true);
         await UserEmailVerificationService.DeleteAsync(emailVerificationEntity.Owner);

         return new OkObjectResult(
            new UserEmailVerificationResponse(true));
      }
   }
}

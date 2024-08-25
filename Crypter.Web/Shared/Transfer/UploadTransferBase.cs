/*
 * Copyright (C) 2024 Crypter File Transfer
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
using Crypter.Common.Client.Enums;
using Crypter.Common.Client.Interfaces.Services;
using Crypter.Common.Client.Transfer;
using Crypter.Common.Client.Transfer.Handlers.Base;
using Crypter.Common.Client.Transfer.Models;
using Crypter.Common.Contracts.Features.Transfer;
using Crypter.Common.Enums;
using Crypter.Web.Services;
using Crypter.Web.Shared.Modal;
using EasyMonads;
using Microsoft.AspNetCore.Components;
using Microsoft.IdentityModel.Tokens;

namespace Crypter.Web.Shared.Transfer;

public class UploadTransferBase : ComponentBase
{
    [Inject] protected IUserSessionService UserSessionService { get; init; } = null!;

    [Inject] protected IUserKeysService UserKeysService { get; init; } = null!;

    [Inject] protected NavigationManager NavigationManager { get; init; } = null!;

    [Inject] protected ClientTransferSettings UploadSettings { get; init; } = null!;

    [Inject] protected TransferHandlerFactory TransferHandlerFactory { get; init; } = null!;

    [Inject] protected IFileSaverService FileSaverService { get; init; } = null!;

    [Parameter] public Maybe<string> RecipientUsername { get; set; }

    [Parameter] public Maybe<byte[]> RecipientPublicKey { get; set; }

    [Parameter] public int ExpirationHours { get; set; }

    [Parameter] public EventCallback UploadCompletedEvent { get; set; }

    [CascadingParameter] public TransferSuccessModal ModalForAnonymousRecipient { get; set; } = null!;

    [CascadingParameter] public BasicModal ModalForUserRecipient { get; set; } = null!;

    protected TransferTransmissionType TransmissionType = TransferTransmissionType.Buffer;
    protected bool EncryptionInProgress = false;
    protected string ErrorMessage = string.Empty;
    protected string UploadStatusMessage = string.Empty;
    protected double? UploadStatusPercent = null;

    private const string UnknownError = "An error occurred.";
    private const string ServerOutOfSpace = "Server is out of space. Try again later.";
    private const string UserNotFound = "User not found.";
    private const string ExpirationRange = "Expiration must be between 1 and 24 hours.";

    protected void SetHandlerUserInfo(IUserUploadHandler handler)
    {
        if (UserSessionService.Session.IsSome)
        {
            byte[] senderPrivateKey = UserKeysService.PrivateKey.Match(
                () => throw new Exception("Missing sender private key"),
                x => x);

            handler.SetSenderInfo(senderPrivateKey);
        }

        RecipientUsername.IfSome(username =>
        {
            byte[] recipientX25519PublicKey = RecipientPublicKey.Match(
                () => throw new Exception("Missing recipient public key"),
                x => x);

            handler.SetRecipientInfo(username, recipientX25519PublicKey);
        });
    }

    protected async Task HandleUploadResponse(Either<UploadTransferError, UploadHandlerResponse> uploadResponse)
    {
        await uploadResponse
            .DoRightAsync(async response =>
            {
                await UploadCompletedEvent.InvokeAsync();

                string itemType = response.ItemType switch
                {
                    TransferItemType.Message => "message",
                    TransferItemType.File => "file",
                    _ => throw new ArgumentOutOfRangeException(nameof(uploadResponse),
                        "Upload response contains an unknown ItemType")
                };

                response.RecipientKeySeed.IfNone(() =>
                {
                    ModalForUserRecipient.Open("Sent", $"Your {itemType} has been sent.", "Ok", Maybe<string>.None,
                        Maybe<EventCallback<bool>>.None);
                });

                response.RecipientKeySeed.IfSome(x =>
                {
                    string recipientKeySeed = Base64UrlEncoder.Encode(x);
                    string downloadUrl =
                        $"{NavigationManager.BaseUri}decrypt/{itemType}/{(int)response.UserType}/{response.TransferId}#{recipientKeySeed}";
                    ModalForAnonymousRecipient.Open(downloadUrl, response.ExpirationHours, UploadCompletedEvent);
                });
            })
            .DoLeftOrNeitherAsync(HandleUploadError, () => HandleUploadError());
    }

    private void HandleUploadError(UploadTransferError error = UploadTransferError.UnknownError)
    {
        ErrorMessage = error switch
        {
            UploadTransferError.UnknownError => UnknownError,
            UploadTransferError.InvalidRequestedLifetimeHours => ExpirationRange,
            UploadTransferError.RecipientNotFound => UserNotFound,
            UploadTransferError.OutOfSpace => ServerOutOfSpace,
            _ => UnknownError
        };
    }
}

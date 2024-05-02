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

using System.Threading.Tasks;
using Crypter.Common.Infrastructure;
using Crypter.Common.Primitives;
using Crypter.Core.Features.Reports.Models;
using Crypter.Core.Models;

namespace Crypter.Core.Services.Email;

public static class EmailServiceTemplateExtensions
{
    /// <summary>
    /// Send a recovery email to the provided recipient, with the provided parameters.
    /// </summary>
    /// <param name="emailService"></param>
    /// <param name="parameters"></param>
    /// <param name="expirationMinutes"></param>
    /// <returns></returns>
    public static async Task<bool> SendAccountRecoveryEmailAsync(this IEmailService emailService, UserRecoveryParameters parameters, int expirationMinutes)
    {
        string encodedUsername = UrlSafeEncoder.EncodeStringUrlSafe(parameters.Username.Value);
        string encodedVerificationCode = UrlSafeEncoder.EncodeGuidUrlSafe(parameters.RecoveryCode);
        string encodedSignature = UrlSafeEncoder.EncodeBytesUrlSafe(parameters.Signature);
        
        string message = $"Click the link below to begin account recovery.\n" +
               $"https://www.crypter.dev/recovery?username={encodedUsername}&code={encodedVerificationCode}&signature={encodedSignature}\n" +
               $"This link will expire in {expirationMinutes} minutes.";
        
        return await emailService.SendAsync("Account recovery", message, parameters.EmailAddress);
    }

    /// <summary>
    /// Send a verification email to the provided recipient.
    /// </summary>
    /// <param name="emailService"></param>
    /// <param name="parameters"></param>
    /// <remarks>This method is 'virtual' to enable some unit tests.</remarks>
    /// <returns></returns>
    public static async Task<bool> SendVerificationEmailAsync(this IEmailService emailService, UserEmailAddressVerificationParameters parameters)
    {
        string encodedVerificationCode = UrlSafeEncoder.EncodeGuidUrlSafe(parameters.VerificationCode);
        string encodedSignature = UrlSafeEncoder.EncodeBytesUrlSafe(parameters.Signature);
        string verificationLink = $"https://www.crypter.dev/verify?code={encodedVerificationCode}&signature={encodedSignature}";

        return await emailService.SendAsync("Verify your email address", verificationLink, parameters.EmailAddress);
    }
    
    /// <summary>
    /// Send an email to the provided recipient, letting them know they received a transfer.
    /// </summary>
    /// <param name="emailService"></param>
    /// <param name="emailAddress"></param>
    /// <returns></returns>
    public static async Task<bool> SendTransferNotificationEmailAsync(this IEmailService emailService, EmailAddress emailAddress)
    {
        return await emailService.SendAsync("Someone sent you a transfer",
            "Someone sent you something on Crypter!  Login to https://www.crypter.dev see what it is.", emailAddress);
    }

    /// <summary>
    /// Send an application analytics report to the provided recipient.
    /// </summary>
    /// <param name="emailService"></param>
    /// <param name="emailAddress"></param>
    /// <param name="reportData"></param>
    /// <returns></returns>
    internal static async Task<bool> SendApplicationAnalyticsReportEmailAsync(this IEmailService emailService, EmailAddress emailAddress, ApplicationAnalyticsReport reportData)
    {
        string message = $"Period: {reportData.Begin} to {reportData.End}\n" +
                         $"Uploads: {reportData.TransferAnalytics.Uploads}\n" +
                         $"Unique previews: {reportData.TransferAnalytics.UniquePreviews}\n" +
                         $"Unique downloads: {reportData.TransferAnalytics.UniqueDownloads}\n" +
                         $"New users: {reportData.UserAnalytics.Registrations}\n" +
                         $"Unique logins: {reportData.UserAnalytics.UniqueLogins}";
        return await emailService.SendAsync("Crypter Analytics", message, emailAddress);
    }
}

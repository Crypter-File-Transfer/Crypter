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
using Crypter.Common.Primitives;
using Crypter.Core.Settings;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Crypter.Core.Services.Email;

public interface IEmailService
{
    Task<bool> SendAsync(string subject, string message, EmailAddress recipient);
}

public sealed class EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
    : IEmailService
{
    /// <summary>
    /// Send an email to the provided recipient.
    /// </summary>
    /// <param name="subject"></param>
    /// <param name="message"></param>
    /// <param name="recipient"></param>
    /// <remarks>This method is 'virtual' to enable some unit tests.</remarks>
    /// <returns></returns>
    public async Task<bool> SendAsync(string subject, string message, EmailAddress recipient)
    {
        if (!emailSettings.Value.Enabled)
        {
            return true;
        }

        MimeMessage mailMessage = new MimeMessage();
        mailMessage.From.Add(MailboxAddress.Parse(emailSettings.Value.From));
        mailMessage.To.Add(MailboxAddress.Parse(recipient.Value));
        mailMessage.Subject = subject;
        mailMessage.Body = new TextPart("plain")
        {
            Text = message
        };

        using SmtpClient smtpClient = new SmtpClient();

        try
        {
            await smtpClient.ConnectAsync(emailSettings.Value.Host, emailSettings.Value.Port, SecureSocketOptions.StartTls);
            await smtpClient.AuthenticateAsync(emailSettings.Value.Username, emailSettings.Value.Password);
            await smtpClient.SendAsync(mailMessage);
        }
        catch (Exception ex)
        {
            logger.LogError("An error occured while sending an email: {error}", ex.Message);
            return false;
        }
        finally
        {
            await smtpClient.DisconnectAsync(true);
        }

        return true;
    }
}

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

using Crypter.Contracts.Features.Transfer;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.Core.Services
{
   public interface IUserTransferService
   {
      Task<UserSentMessagesResponse> GetUserSentMessagesAsync(Guid userId, CancellationToken cancellationToken);
      Task<UserReceivedMessagesResponse> GetUserReceivedMessagesAsync(Guid userId, CancellationToken cancellationToken);
      Task<UserSentFilesResponse> GetUserSentFilesAsync(Guid userId, CancellationToken cancellationToken);
      Task<UserReceivedFilesResponse> GetUserReceivedFilesAsync(Guid userId, CancellationToken cancellationToken);
   }

   public class UserTransferService : IUserTransferService
   {
      private readonly DataContext _context;

      public UserTransferService(DataContext context)
      {
         _context = context;
      }

      public async Task<UserSentMessagesResponse> GetUserSentMessagesAsync(Guid userId, CancellationToken cancellationToken)
      {
         var sentMessages = await _context.UserMessageTransfers
            .Where(x => x.SenderId == userId)
            .OrderBy(x => x.Expiration)
            .Select(x => new UserSentMessageDTO(x.Id, x.Subject, x.Recipient.Username, x.Recipient.Profile.Alias, x.Expiration))
            .ToListAsync(cancellationToken);

         return new UserSentMessagesResponse(sentMessages);
      }

      public async Task<UserReceivedMessagesResponse> GetUserReceivedMessagesAsync(Guid userId, CancellationToken cancellationToken)
      {
         var receivedMessages = await _context.UserMessageTransfers
            .Where(x => x.RecipientId == userId)
            .OrderBy(x => x.Expiration)
            .Select(x => new UserReceivedMessageDTO(x.Id, x.Subject, x.Sender.Username, x.Sender.Profile.Alias, x.Expiration))
            .ToListAsync(cancellationToken);

         return new UserReceivedMessagesResponse(receivedMessages);
      }

      public async Task<UserSentFilesResponse> GetUserSentFilesAsync(Guid userId, CancellationToken cancellationToken)
      {
         var sentFiles = await _context.UserFileTransfers
            .Where(x => x.SenderId == userId)
            .OrderBy(x => x.Expiration)
            .Select(x => new UserSentFileDTO(x.Id, x.FileName, x.Recipient.Username, x.Recipient.Profile.Alias, x.Expiration))
            .ToListAsync(cancellationToken);

         return new UserSentFilesResponse(sentFiles);
      }

      public async Task<UserReceivedFilesResponse> GetUserReceivedFilesAsync(Guid userId, CancellationToken cancellationToken)
      {
         var receivedFiles = await _context.UserFileTransfers
            .Where(x => x.RecipientId == userId)
            .OrderBy(x => x.Expiration)
            .Select(x => new UserReceivedFileDTO(x.Id, x.FileName, x.Sender.Username, x.Sender.Profile.Alias, x.Expiration))
            .ToListAsync(cancellationToken);

         return new UserReceivedFilesResponse(receivedFiles);
      }
   }
}

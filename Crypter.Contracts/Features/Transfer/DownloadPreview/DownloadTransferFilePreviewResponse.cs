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

using System;
using System.Text.Json.Serialization;

namespace Crypter.Contracts.Features.Transfer.DownloadPreview
{
   public class DownloadTransferFilePreviewResponse
   {
      public string FileName { get; set; }
      public string ContentType { get; set; }
      public int Size { get; set; }
      public Guid SenderId { get; set; }
      public string SenderUsername { get; set; }
      public string SenderAlias { get; set; }
      public Guid RecipientId { get; set; }
      public string X25519PublicKey { get; set; }
      public DateTime CreationUTC { get; set; }
      public DateTime ExpirationUTC { get; set; }

      [JsonConstructor]
      public DownloadTransferFilePreviewResponse(string fileName, string contentType, int size, Guid senderId, string senderUsername, string senderAlias, Guid recipientId, string x25519PublicKey, DateTime creationUTC, DateTime expirationUTC)
      {
         FileName = fileName;
         ContentType = contentType;
         Size = size;
         SenderId = senderId;
         SenderUsername = senderUsername;
         SenderAlias = senderAlias;
         RecipientId = recipientId;
         X25519PublicKey = x25519PublicKey;
         CreationUTC = creationUTC;
         ExpirationUTC = expirationUTC;
      }
   }
}

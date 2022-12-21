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

using Crypter.Core.Entities.Interfaces;
using System;

namespace Crypter.Core.Entities
{
   public class AnonymousFileTransferEntity : IFileTransfer
   {
      public Guid Id { get; set; }
      public long Size { get; set; }
      public byte[] PublicKey { get; set; }
      public byte[] KeyExchangeNonce { get; set; }
      public byte[] Proof { get; set; }
      public DateTime Created { get; set; }
      public DateTime Expiration { get; set; }

      // IFileTransfer
      public string FileName { get; set; }
      public string ContentType { get; set; }

      public AnonymousFileTransferEntity(Guid id, long size, byte[] publicKey, byte[] keyExchangeNonce, byte[] proof, DateTime created, DateTime expiration, string fileName, string contentType)
      {
         Id = id;
         Size = size;
         PublicKey = publicKey;
         KeyExchangeNonce = keyExchangeNonce;
         Proof = proof;
         Created = created;
         Expiration = expiration;
         FileName = fileName;
         ContentType = contentType;
      }
   }
}

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

using Crypter.Core.Interfaces;
using System;

namespace Crypter.Core.Models
{
   /// <summary>
   /// This class does not represent an entity tracked in the database.
   /// </summary>
   public class BaseTransfer : IBaseTransferItem
   {
      public Guid Id { get; set; }
      public Guid Sender { get; set; }
      public Guid Recipient { get; set; }
      public int Size { get; set; }
      public string ClientIV { get; set; }
      public string Signature { get; set; }
      public string X25519PublicKey { get; set; }
      public string Ed25519PublicKey { get; set; }
      public byte[] ServerIV { get; set; }
      public byte[] ServerDigest { get; set; }
      public DateTime Created { get; set; }
      public DateTime Expiration { get; set; }

      public BaseTransfer(Guid id, Guid sender, Guid recipient, int size, string clientIV, string signature, string x25519PublicKey, string ed25519PublicKey, byte[] serverIV, byte[] serverDigest, DateTime created, DateTime expiration)
      {
         Id = id;
         Sender = sender;
         Recipient = recipient;
         Size = size;
         ClientIV = clientIV;
         Signature = signature;
         X25519PublicKey = x25519PublicKey;
         Ed25519PublicKey = ed25519PublicKey;
         ServerIV = serverIV;
         ServerDigest = serverDigest;
         Created = created;
         Expiration = expiration;
      }
   }
}

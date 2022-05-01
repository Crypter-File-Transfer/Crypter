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
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Crypter.Core.Entities
{
   [Table("UserEd25519KeyPair")]
   public class UserEd25519KeyPairEntity : IUserPublicKeyPair
   {
      [Key]
      public Guid Id { get; set; }
      [ForeignKey("User")]
      public Guid Owner { get; set; }
      public string PrivateKey { get; set; }
      public string PublicKey { get; set; }
      public string ClientIV { get; set; }
      public DateTime Created { get; set; }

      public virtual UserEntity User { get; set; }

      public UserEd25519KeyPairEntity(Guid id, Guid owner, string privateKey, string publicKey, string clientIV, DateTime created)
      {
         Id = id;
         Owner = owner;
         PrivateKey = privateKey;
         PublicKey = publicKey;
         ClientIV = clientIV;
         Created = created;
      }
   }
}
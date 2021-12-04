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

using Newtonsoft.Json;
using System;

namespace Crypter.Contracts.Responses
{
   public class UserAuthenticateResponse
   {
      public Guid Id { get; set; }
      public string Token { get; set; }
      public string EncryptedX25519PrivateKey { get; set; }
      public string EncryptedEd25519PrivateKey { get; set; }

      [JsonConstructor]
      public UserAuthenticateResponse(Guid id, string token, string encryptedX25519PrivateKey = null, string encryptedEd25519PrivateKey = null)
      {
         Id = id;
         Token = token;
         EncryptedX25519PrivateKey = encryptedX25519PrivateKey;
         EncryptedEd25519PrivateKey = encryptedEd25519PrivateKey;
      }
   }
}


﻿/*
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

namespace Crypter.Contracts.Features.Authentication.Login
{
   public class LoginResponse
   {
      public Guid Id { get; set; }
      public string AuthenticationToken { get; set; }
      public string RefreshToken { get; set; }
      public string EncryptedX25519PrivateKey { get; set; }
      public string EncryptedEd25519PrivateKey { get; set; }
      public string X25519IV { get; set; }
      public string Ed25519IV { get; set; }

      [JsonConstructor]
      public LoginResponse(Guid id, string authenticationToken, string refreshToken = null, string encryptedX25519PrivateKey = null, string encryptedEd25519PrivateKey = null, string x25519IV = null, string ed25519IV = null)
      {
         Id = id;
         AuthenticationToken = authenticationToken;
         RefreshToken = refreshToken;
         EncryptedX25519PrivateKey = encryptedX25519PrivateKey;
         EncryptedEd25519PrivateKey = encryptedEd25519PrivateKey;
         X25519IV = x25519IV;
         Ed25519IV = ed25519IV;
      }
   }
}

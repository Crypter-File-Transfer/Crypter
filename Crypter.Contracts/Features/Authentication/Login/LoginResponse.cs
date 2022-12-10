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

using System.Text.Json.Serialization;

namespace Crypter.Contracts.Features.Authentication
{
   public class LoginResponse
   {
      public string Username { get; init; }
      public string AuthenticationToken { get; init; }
      public string RefreshToken { get; init; }
      public bool UploadNewKeys { get; init; }
      public bool ShowRecoveryKey { get; init; }

      [JsonConstructor]
      public LoginResponse(string username, string authenticationToken, string refreshToken, bool uploadNewKeys, bool showRecoveryKey)
      {
         Username = username;
         AuthenticationToken = authenticationToken;
         RefreshToken = refreshToken;
         UploadNewKeys = uploadNewKeys;
         ShowRecoveryKey = showRecoveryKey;
      }
   }
}

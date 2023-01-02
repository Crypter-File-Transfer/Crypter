/*
 * Copyright (C) 2023 Crypter File Transfer
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

using Crypter.Common.Enums;
using System;
using System.Text.Json.Serialization;

namespace Crypter.Common.Contracts.Features.Settings
{
   public class UserSettingsResponse
   {
      public string Username { get; set; }
      public string EmailAddress { get; set; }
      public bool EmailVerified { get; set; }
      public string Alias { get; set; }
      public string About { get; set; }
      public UserVisibilityLevel Visibility { get; set; }
      public bool AllowKeyExchangeRequests { get; set; }
      public UserItemTransferPermission MessageTransferPermission { get; set; }
      public UserItemTransferPermission FileTransferPermission { get; set; }
      public bool EnableTransferNotifications { get; set; }
      public bool EmailNotifications { get; set; }
      public DateTime UserCreated { get; set; }

      [JsonConstructor]
      public UserSettingsResponse(string username, string emailAddress, bool emailVerified, string alias, string about,
         UserVisibilityLevel visibility, bool allowKeyExchangeRequests, UserItemTransferPermission messageTransferPermission, UserItemTransferPermission fileTransferPermission,
         bool enableTransferNotifications, bool emailNotifications, DateTime userCreated)
      {
         Username = username;
         EmailAddress = emailAddress;
         EmailVerified = emailVerified;
         Alias = alias;
         About = about;
         Visibility = visibility;
         AllowKeyExchangeRequests = allowKeyExchangeRequests;
         MessageTransferPermission = messageTransferPermission;
         FileTransferPermission = fileTransferPermission;
         EnableTransferNotifications = enableTransferNotifications;
         EmailNotifications = emailNotifications;
         UserCreated = userCreated;
      }
   }
}

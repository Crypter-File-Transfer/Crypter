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

namespace Crypter.Contracts.Features.User.GetPublicProfile
{
   public class UserPublicProfileResponse
   {
      public Guid Id { get; set; }
      public string Username { get; set; }
      public string Alias { get; set; }
      public string About { get; set; }
      public bool AllowKeyExchangeRequests { get; set; }
      public bool Visible { get; set; }
      public bool ReceivesMessages { get; set; }
      public bool ReceivesFiles { get; set; }
      public string PublicDHKey { get; set; }
      public string PublicDSAKey { get; set; }

      [JsonConstructor]
      public UserPublicProfileResponse(Guid id, string username, string alias, string about, bool allowKeyExchangeRequests, bool visible, bool receivesMessages, bool receivesFiles, string publicDHKey, string publicDSAKey)
      {
         Id = id;
         Username = username;
         Alias = alias;
         About = about;
         AllowKeyExchangeRequests = allowKeyExchangeRequests;
         Visible = visible;
         ReceivesMessages = receivesMessages;
         ReceivesFiles = receivesFiles;
         PublicDHKey = publicDHKey;
         PublicDSAKey = publicDSAKey;
      }
   }
}
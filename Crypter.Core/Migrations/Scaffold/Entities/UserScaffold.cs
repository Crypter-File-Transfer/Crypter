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

using System;
using System.Collections.Generic;

namespace Crypter.Core.Migrations.Scaffold.Entities
{
   public partial class UserScaffold
   {
      public UserScaffold()
      {
         UserContactContactNavigations = new HashSet<UserContactScaffold>();
         UserContactOwnerNavigations = new HashSet<UserContactScaffold>();
         UserEd25519KeyPairs = new HashSet<UserEd25519KeyPairScaffold>();
         UserTokens = new HashSet<UserTokenScaffold>();
         UserX25519keyPairs = new HashSet<UserX25519KeyPairScaffold>();
      }

      public Guid Id { get; set; }
      public string Username { get; set; }
      public string Email { get; set; }
      public byte[] PasswordHash { get; set; }
      public byte[] PasswordSalt { get; set; }
      public bool EmailVerified { get; set; }
      public DateTime Created { get; set; }
      public DateTime LastLogin { get; set; }

      public virtual UserEmailVerificationScaffold UserEmailVerification { get; set; }
      public virtual UserNotificationSettingScaffold UserNotificationSetting { get; set; }
      public virtual UserPrivacySettingScaffold UserPrivacySetting { get; set; }
      public virtual UserProfileScaffold UserProfile { get; set; }
      public virtual ICollection<UserContactScaffold> UserContactContactNavigations { get; set; }
      public virtual ICollection<UserContactScaffold> UserContactOwnerNavigations { get; set; }
      public virtual ICollection<UserEd25519KeyPairScaffold> UserEd25519KeyPairs { get; set; }
      public virtual ICollection<UserTokenScaffold> UserTokens { get; set; }
      public virtual ICollection<UserX25519KeyPairScaffold> UserX25519keyPairs { get; set; }
   }
}

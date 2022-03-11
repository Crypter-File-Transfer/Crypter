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

using Crypter.Common.Monads;
using Crypter.Common.Primitives;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Crypter.Core.Models
{
   [Table("User")]
   public class User
   {
      [Key]
      public Guid Id { get; set; }
      public string Username { get; set; }
      public string Email { get; set; }
      public byte[] PasswordHash { get; set; }
      public byte[] PasswordSalt { get; set; }
      public bool EmailVerified { get; set; }
      public DateTime Created { get; set; }
      public DateTime LastLogin { get; set; }

      public virtual UserProfile Profile { get; set; }
      public virtual UserPrivacySetting PrivacySetting { get; set; }
      public virtual UserNotificationSetting NotificationSetting { get; set; }
      public virtual UserEd25519KeyPair Ed25519KeyPair { get; set; }
      public virtual UserX25519KeyPair X25519KeyPair { get; set; }
      public virtual List<UserToken> Tokens { get; set; }
      public virtual List<UserContact> Contacts { get; set; }

      /// <summary>
      /// Don't use this
      /// </summary>
      /// <param name="id"></param>
      /// <param name="username"></param>
      /// <param name="email"></param>
      /// <param name="passwordHash"></param>
      /// <param name="passwordSalt"></param>
      /// <param name="emailVerified"></param>
      /// <param name="created"></param>
      /// <param name="lastLogin"></param>
      public User(Guid id, string username, string email, byte[] passwordHash, byte[] passwordSalt, bool emailVerified, DateTime created, DateTime lastLogin)
      {
         Id = id;
         Username = username;
         Email = email;
         PasswordHash = passwordHash;
         PasswordSalt = passwordSalt;
         EmailVerified = emailVerified;
         Created = created;
         LastLogin = lastLogin;
      }

      public User(Guid id, Username username, Maybe<EmailAddress> email, byte[] passwordHash, byte[] passwordSalt, bool emailVerified, DateTime created, DateTime lastLogin)
      {
         Id = id;
         Username = username.Value;
         Email = email.Match(
            () => null,
            some => some.Value);
         PasswordHash = passwordHash;
         PasswordSalt = passwordSalt;
         EmailVerified = emailVerified;
         Created = created;
         LastLogin = lastLogin;
      }
   }
}

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

using System;
using System.Collections.Generic;
using Crypter.Common.Primitives;
using EasyMonads;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Crypter.Core.Entities
{
   public class UserEntity
   {
      public Guid Id { get; set; }
      public string Username { get; set; }
      public string EmailAddress { get; set; }
      public byte[] PasswordHash { get; set; }
      public byte[] PasswordSalt { get; set; }
      public short ServerPasswordVersion { get; set; }
      public short ClientPasswordVersion { get; set; }
      public bool EmailVerified { get; set; }
      public DateTime Created { get; set; }
      public DateTime LastLogin { get; set; }

      public UserProfileEntity Profile { get; set; }
      public UserPrivacySettingEntity PrivacySetting { get; set; }
      public UserRecoveryEntity Recovery { get; set; }
      public UserEmailVerificationEntity EmailVerification { get; set; }
      public UserNotificationSettingEntity NotificationSetting { get; set; }
      public UserKeyPairEntity KeyPair { get; set; }
      public UserMasterKeyEntity MasterKey { get; set; }
      public List<UserTokenEntity> Tokens { get; set; }
      public List<UserContactEntity> Contacts { get; set; }
      public List<UserContactEntity> Contactors { get; set; }
      public List<UserFileTransferEntity> SentFileTransfers { get; set; }
      public List<UserFileTransferEntity> ReceivedFileTransfers { get; set; }
      public List<UserMessageTransferEntity> SentMessageTransfers { get; set; }
      public List<UserMessageTransferEntity> ReceivedMessageTransfers { get; set; }
      public List<UserFailedLoginEntity> FailedLoginAttempts { get; set; }
      public List<UserConsentEntity> Consents { get; set; }

      /// <summary>
      /// Please avoid using this.
      /// This is only intended to be used by Entity Framework.
      /// </summary>
      public UserEntity(Guid id, string username, string emailAddress, byte[] passwordHash, byte[] passwordSalt, short serverPasswordVersion, short clientPasswordVersion, bool emailVerified, DateTime created, DateTime lastLogin)
      {
         Id = id;
         Username = username;
         EmailAddress = emailAddress;
         PasswordHash = passwordHash;
         PasswordSalt = passwordSalt;
         ServerPasswordVersion = serverPasswordVersion;
         ClientPasswordVersion = clientPasswordVersion;
         EmailVerified = emailVerified;
         Created = created;
         LastLogin = lastLogin;
      }

      public UserEntity(Guid id, Username username, Maybe<EmailAddress> emailAddress, byte[] passwordHash, byte[] passwordSalt, short serverPasswordVersion, short clientPasswordVersion, bool emailVerified, DateTime created, DateTime lastLogin)
      {
         Id = id;
         Username = username.Value;
         EmailAddress = emailAddress.Match(
            () => null,
            some => some.Value);
         PasswordHash = passwordHash;
         PasswordSalt = passwordSalt;
         ServerPasswordVersion = serverPasswordVersion;
         ClientPasswordVersion = clientPasswordVersion;
         EmailVerified = emailVerified;
         Created = created;
         LastLogin = lastLogin;
      }
   }

   public class UserEntityConfiguration : IEntityTypeConfiguration<UserEntity>
   {
      public void Configure(EntityTypeBuilder<UserEntity> builder)
      {
         builder.ToTable("User");

         builder.HasKey(x => x.Id);

         builder.Property(x => x.Username)
            .HasColumnType("citext");

         builder.Property(x => x.EmailAddress)
            .HasColumnType("citext");

         builder.Property(x => x.ServerPasswordVersion)
            .IsRequired()
            .HasDefaultValue(0);

         builder.Property(x => x.ClientPasswordVersion)
            .IsRequired()
            .HasDefaultValue(0);

         builder.HasMany(x => x.Contacts)
            .WithOne(x => x.Owner);

         builder.HasMany(x => x.SentFileTransfers)
            .WithOne(x => x.Sender)
            .HasForeignKey(x => x.SenderId);

         builder.HasMany(x => x.ReceivedFileTransfers)
            .WithOne(x => x.Recipient)
            .HasForeignKey(x => x.RecipientId);

         builder.HasMany(x => x.SentMessageTransfers)
            .WithOne(x => x.Sender)
            .HasForeignKey(x => x.SenderId);

         builder.HasMany(x => x.ReceivedMessageTransfers)
            .WithOne(x => x.Recipient)
            .HasForeignKey(x => x.RecipientId);

         builder.HasIndex(x => x.Username)
            .IsUnique();

         builder.HasIndex(x => x.EmailAddress)
            .IsUnique();
      }
   }
}

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

using Crypter.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Crypter.Core
{
   public class DataContext : DbContext
   {
      protected readonly IConfiguration Configuration;

      public DataContext(IConfiguration configuration)
      {
         Configuration = configuration;
      }

      protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
      {
         var connectionString = Configuration.GetConnectionString("DefaultConnection");
         optionsBuilder.UseNpgsql(connectionString);
      }

      public DbSet<UserEntity> Users { get; set; }
      public DbSet<UserProfileEntity> UserProfiles { get; set; }
      public DbSet<UserEd25519KeyPairEntity> UserEd25519KeyPairs { get; set; }
      public DbSet<UserX25519KeyPairEntity> UserX25519KeyPairs { get; set; }
      public DbSet<UserPrivacySettingEntity> UserPrivacySettings { get; set; }
      public DbSet<UserEmailVerificationEntity> UserEmailVerifications { get; set; }
      public DbSet<UserNotificationSettingEntity> UserNotificationSettings { get; set; }
      public DbSet<UserTokenEntity> UserTokens { get; set; }
      public DbSet<UserContactEntity> UserContacts { get; set; }
      public DbSet<AnonymousFileTransferEntity> AnonymousFileTransfers { get; set; }
      public DbSet<AnonymousMessageTransferEntity> AnonymousMessageTransfers { get; set; }
      public DbSet<UserFileTransferEntity> UserFileTransfers { get; set; }
      public DbSet<UserMessageTransferEntity> UserMessageTransfers { get; set; }
      public DbSet<UserFailedLoginEntity> UserFailedLoginAttempts { get; set; }
      public DbSet<UserMasterKeyEntity> UserMasterKeys { get; set; }

      protected override void OnModelCreating(ModelBuilder builder)
      {
         builder.HasPostgresExtension("citext");

         ConfigureUserEntity(builder);
         ConfigureUserProfileEntity(builder);
         ConfigureUserEd25519KeyPairEntity(builder);
         ConfigureUserX25519KeyPairEntity(builder);
         ConfigureUserPrivacySettingEntity(builder);
         ConfigureUserEmailVerificationEntity(builder);
         ConfigureUserNotificationSettingsEntity(builder);
         ConfigureUserTokenEntity(builder);
         ConfigureUserContactEntity(builder);
         ConfigureUserMessageTransferEntity(builder);
         ConfigureUserFileTransferEntity(builder);
         ConfigureAnonymousMessageTransferEntity(builder);
         ConfigureAnonymousFileTransferEntity(builder);
         ConfigureUserFailedLoginEntity(builder);
         ConfigureUserMasterKeyEntity(builder);
      }

      private static void ConfigureUserEntity(ModelBuilder builder)
      {
         builder.Entity<UserEntity>()
            .ToTable("User");

         builder.Entity<UserEntity>()
            .HasKey(x => x.Id);

         builder.Entity<UserEntity>()
            .Property(x => x.Username)
            .HasColumnType("citext");

         builder.Entity<UserEntity>()
            .Property(x => x.EmailAddress)
            .HasColumnType("citext");

         builder.Entity<UserEntity>()
            .HasMany(x => x.Contacts)
            .WithOne(x => x.Owner);

         builder.Entity<UserEntity>()
            .HasMany(x => x.SentFileTransfers)
            .WithOne(x => x.Sender)
            .HasForeignKey(x => x.SenderId);

         builder.Entity<UserEntity>()
            .HasMany(x => x.ReceivedFileTransfers)
            .WithOne(x => x.Recipient)
            .HasForeignKey(x => x.RecipientId);

         builder.Entity<UserEntity>()
            .HasMany(x => x.SentMessageTransfers)
            .WithOne(x => x.Sender)
            .HasForeignKey(x => x.SenderId);

         builder.Entity<UserEntity>()
            .HasMany(x => x.ReceivedMessageTransfers)
            .WithOne(x => x.Recipient)
            .HasForeignKey(x => x.RecipientId);

         builder.Entity<UserEntity>()
            .HasIndex(x => x.Username)
            .IsUnique();

         builder.Entity<UserEntity>()
            .HasIndex(x => x.EmailAddress)
            .IsUnique();
      }

      private static void ConfigureUserProfileEntity(ModelBuilder builder)
      {
         builder.Entity<UserProfileEntity>()
            .ToTable("UserProfile");

         builder.Entity<UserProfileEntity>()
            .HasKey(x => x.Owner);

         builder.Entity<UserProfileEntity>()
            .HasOne(x => x.User)
            .WithOne(x => x.Profile)
            .HasForeignKey<UserProfileEntity>(x => x.Owner)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
      }

      private static void ConfigureUserEd25519KeyPairEntity(ModelBuilder builder)
      {
         builder.Entity<UserEd25519KeyPairEntity>()
            .ToTable("UserEd25519KeyPair");

         builder.Entity<UserEd25519KeyPairEntity>()
            .HasKey(x => x.Owner);

         builder.Entity<UserEd25519KeyPairEntity>()
            .HasOne(x => x.User)
            .WithOne(x => x.Ed25519KeyPair)
            .HasForeignKey<UserEd25519KeyPairEntity>(x => x.Owner)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);
      }

      private static void ConfigureUserX25519KeyPairEntity(ModelBuilder builder)
      {
         builder.Entity<UserX25519KeyPairEntity>()
            .ToTable("UserX25519KeyPair");

         builder.Entity<UserX25519KeyPairEntity>()
            .HasKey(x => x.Owner);

         builder.Entity<UserX25519KeyPairEntity>()
            .HasOne(x => x.User)
            .WithOne(x => x.X25519KeyPair)
            .HasForeignKey<UserX25519KeyPairEntity>(x => x.Owner)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);
      }

      private static void ConfigureUserPrivacySettingEntity(ModelBuilder builder)
      {
         builder.Entity<UserPrivacySettingEntity>()
            .ToTable("UserPrivacySetting");

         builder.Entity<UserPrivacySettingEntity>()
            .HasKey(x => x.Owner);

         builder.Entity<UserPrivacySettingEntity>()
            .HasOne(x => x.User)
            .WithOne(x => x.PrivacySetting)
            .HasForeignKey<UserPrivacySettingEntity>(x => x.Owner)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
      }

      private static void ConfigureUserEmailVerificationEntity(ModelBuilder builder)
      {
         builder.Entity<UserEmailVerificationEntity>()
            .ToTable("UserEmailVerification");

         builder.Entity<UserEmailVerificationEntity>()
            .HasKey(x => x.Owner);

         builder.Entity<UserEmailVerificationEntity>()
            .HasOne(x => x.User)
            .WithOne(x => x.EmailVerification)
            .HasForeignKey<UserEmailVerificationEntity>(x => x.Owner)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);
      }

      private static void ConfigureUserNotificationSettingsEntity(ModelBuilder builder)
      {
         builder.Entity<UserNotificationSettingEntity>()
            .ToTable("UserNotificationSetting");

         builder.Entity<UserNotificationSettingEntity>()
            .HasKey(x => x.Owner);

         builder.Entity<UserNotificationSettingEntity>()
            .HasOne(x => x.User)
            .WithOne(x => x.NotificationSetting)
            .HasForeignKey<UserNotificationSettingEntity>(x => x.Owner)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
      }

      private static void ConfigureUserTokenEntity(ModelBuilder builder)
      {
         builder.Entity<UserTokenEntity>()
            .ToTable("UserToken");

         builder.Entity<UserTokenEntity>()
            .HasKey(x => x.Id);

         builder.Entity<UserTokenEntity>()
            .HasOne(x => x.User)
            .WithMany(x => x.Tokens)
            .HasForeignKey(x => x.Owner)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
      }

      private static void ConfigureUserContactEntity(ModelBuilder builder)
      {
         builder.Entity<UserContactEntity>()
            .ToTable("UserContact");

         builder.Entity<UserContactEntity>()
            .HasKey(x => new { x.OwnerId, x.ContactId });

         builder.Entity<UserContactEntity>()
            .Property(x => x.OwnerId)
            .HasColumnName("Owner");

         builder.Entity<UserContactEntity>()
            .Property(x => x.ContactId)
            .HasColumnName("Contact");

         builder.Entity<UserContactEntity>()
            .HasOne(x => x.Owner)
            .WithMany(x => x.Contacts)
            .HasForeignKey(x => x.OwnerId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

         builder.Entity<UserContactEntity>()
            .HasOne(x => x.Contact)
            .WithMany(x => x.Contactors)
            .HasForeignKey(x => x.ContactId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
      }

      private static void ConfigureUserMessageTransferEntity(ModelBuilder builder)
      {
         builder.Entity<UserMessageTransferEntity>()
            .ToTable("UserMessageTransfer");

         builder.Entity<UserMessageTransferEntity>()
            .HasKey(x => x.Id);

         builder.Entity<UserMessageTransferEntity>()
            .Property(x => x.SenderId)
            .HasColumnName("Sender");

         builder.Entity<UserMessageTransferEntity>()
            .Property(x => x.RecipientId)
            .HasColumnName("Recipient");

         builder.Entity<UserMessageTransferEntity>()
            .Property(x => x.DigitalSignature)
            .IsRequired();

         builder.Entity<UserMessageTransferEntity>()
            .Property(x => x.DigitalSignaturePublicKey)
            .IsRequired();

         builder.Entity<UserMessageTransferEntity>()
            .Property(x => x.DiffieHellmanPublicKey)
            .IsRequired();

         builder.Entity<UserMessageTransferEntity>()
            .Property(x => x.RecipientProof)
            .IsRequired();

         builder.Entity<UserMessageTransferEntity>()
            .Property(x => x.Subject)
            .IsRequired();
      }

      private static void ConfigureUserFileTransferEntity(ModelBuilder builder)
      {
         builder.Entity<UserFileTransferEntity>()
            .ToTable("UserFileTransfer");

         builder.Entity<UserFileTransferEntity>()
            .HasKey(x => x.Id);

         builder.Entity<UserFileTransferEntity>()
            .Property(x => x.SenderId)
            .HasColumnName("Sender");

         builder.Entity<UserFileTransferEntity>()
            .Property(x => x.RecipientId)
            .HasColumnName("Recipient");

         builder.Entity<UserFileTransferEntity>()
            .Property(x => x.DigitalSignature)
            .IsRequired();

         builder.Entity<UserFileTransferEntity>()
            .Property(x => x.DigitalSignaturePublicKey)
            .IsRequired();

         builder.Entity<UserFileTransferEntity>()
            .Property(x => x.DiffieHellmanPublicKey)
            .IsRequired();

         builder.Entity<UserFileTransferEntity>()
            .Property(x => x.RecipientProof)
            .IsRequired();

         builder.Entity<UserFileTransferEntity>()
            .Property(x => x.FileName)
            .IsRequired();

         builder.Entity<UserFileTransferEntity>()
            .Property(x => x.ContentType)
            .IsRequired();
      }

      private static void ConfigureAnonymousMessageTransferEntity(ModelBuilder builder)
      {
         builder.Entity<AnonymousMessageTransferEntity>()
            .ToTable("AnonymousMessageTransfer");

         builder.Entity<AnonymousMessageTransferEntity>()
            .HasKey(x => x.Id);

         builder.Entity<AnonymousMessageTransferEntity>()
            .Property(x => x.DigitalSignature)
            .IsRequired();

         builder.Entity<AnonymousMessageTransferEntity>()
            .Property(x => x.DigitalSignaturePublicKey)
            .IsRequired();

         builder.Entity<AnonymousMessageTransferEntity>()
            .Property(x => x.DiffieHellmanPublicKey)
            .IsRequired();

         builder.Entity<AnonymousMessageTransferEntity>()
            .Property(x => x.RecipientProof)
            .IsRequired();

         builder.Entity<AnonymousMessageTransferEntity>()
            .Property(x => x.Subject)
            .IsRequired();
      }

      private static void ConfigureAnonymousFileTransferEntity(ModelBuilder builder)
      {
         builder.Entity<AnonymousFileTransferEntity>()
            .ToTable("AnonymousFileTransfer");

         builder.Entity<AnonymousFileTransferEntity>()
            .HasKey(x => x.Id);

         builder.Entity<AnonymousFileTransferEntity>()
            .Property(x => x.DigitalSignature)
            .IsRequired();

         builder.Entity<AnonymousFileTransferEntity>()
            .Property(x => x.DigitalSignaturePublicKey)
            .IsRequired();

         builder.Entity<AnonymousFileTransferEntity>()
            .Property(x => x.DiffieHellmanPublicKey)
            .IsRequired();

         builder.Entity<AnonymousFileTransferEntity>()
            .Property(x => x.RecipientProof)
            .IsRequired();

         builder.Entity<AnonymousFileTransferEntity>()
            .Property(x => x.FileName)
            .IsRequired();

         builder.Entity<AnonymousFileTransferEntity>()
            .Property(x => x.ContentType)
            .IsRequired();
      }

      private static void ConfigureUserFailedLoginEntity(ModelBuilder builder)
      {
         builder.Entity<UserFailedLoginEntity>()
            .ToTable("UserFailedLogin");

         builder.Entity<UserFailedLoginEntity>()
            .HasKey(x => x.Id);

         builder.Entity<UserFailedLoginEntity>()
            .HasOne(x => x.User)
            .WithMany(x => x.FailedLoginAttempts)
            .HasForeignKey(x => x.Owner)
            .IsRequired(true)
            .OnDelete(DeleteBehavior.Cascade);
      }

      private static void ConfigureUserMasterKeyEntity(ModelBuilder builder)
      {
         builder.Entity<UserMasterKeyEntity>()
            .ToTable("UserSymmetricKey");

         builder.Entity<UserMasterKeyEntity>()
            .HasKey(x => x.Owner);

         builder.Entity<UserMasterKeyEntity>()
            .HasOne(x => x.User)
            .WithOne(x => x.MasterKey)
            .HasForeignKey<UserMasterKeyEntity>(x => x.Owner)
            .OnDelete(DeleteBehavior.Cascade);
      }
   }
}

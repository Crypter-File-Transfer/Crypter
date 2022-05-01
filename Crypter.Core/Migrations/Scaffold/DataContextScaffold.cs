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

using Crypter.Core.Migrations.Scaffold.Entities;
using Microsoft.EntityFrameworkCore;

namespace Crypter.Core.Migrations.Scaffold
{
   public partial class DataContextScaffold : DbContext
   {
      public DataContextScaffold()
      {
      }

      public DataContextScaffold(DbContextOptions<DataContextScaffold> options)
          : base(options)
      {
      }

      public virtual DbSet<FileTransferScaffold> FileTransfers { get; set; }
      public virtual DbSet<MessageTransferScaffold> MessageTransfers { get; set; }
      public virtual DbSet<SchemaScaffold> Schemas { get; set; }
      public virtual DbSet<UserScaffold> Users { get; set; }
      public virtual DbSet<UserContactScaffold> UserContacts { get; set; }
      public virtual DbSet<UserEd25519KeyPairScaffold> UserEd25519KeyPairs { get; set; }
      public virtual DbSet<UserEmailVerificationScaffold> UserEmailVerifications { get; set; }
      public virtual DbSet<UserNotificationSettingScaffold> UserNotificationSettings { get; set; }
      public virtual DbSet<UserPrivacySettingScaffold> UserPrivacySettings { get; set; }
      public virtual DbSet<UserProfileScaffold> UserProfiles { get; set; }
      public virtual DbSet<UserTokenScaffold> UserTokens { get; set; }
      public virtual DbSet<UserX25519KeyPairScaffold> UserX25519keyPairs { get; set; }

      protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
      {
         if (!optionsBuilder.IsConfigured)
         {
            optionsBuilder.UseNpgsql("host=127.0.0.1;database=crypter;user id=postgres;pwd=CHANGE_ME;");
         }
      }

      protected override void OnModelCreating(ModelBuilder modelBuilder)
      {
         modelBuilder.Entity<FileTransferScaffold>(entity =>
         {
            entity.ToTable("FileTransfer");

            entity.HasIndex(e => e.Recipient, "Idx_FileTransfer_Recipient");

            entity.HasIndex(e => e.Sender, "Idx_FileTransfer_Sender");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.Property(e => e.ClientIv).HasColumnName("ClientIV");

            entity.Property(e => e.Created).HasColumnType("timestamp without time zone");

            entity.Property(e => e.Expiration).HasColumnType("timestamp without time zone");

            entity.Property(e => e.ServerIv).HasColumnName("ServerIV");

            entity.Property(e => e.X25519publicKey).HasColumnName("X25519PublicKey");
         });

         modelBuilder.Entity<MessageTransferScaffold>(entity =>
         {
            entity.ToTable("MessageTransfer");

            entity.HasIndex(e => e.Recipient, "Idx_MessageTransfer_Recipient");

            entity.HasIndex(e => e.Sender, "Idx_MessageTransfer_Sender");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.Property(e => e.ClientIv).HasColumnName("ClientIV");

            entity.Property(e => e.Created).HasColumnType("timestamp without time zone");

            entity.Property(e => e.Expiration).HasColumnType("timestamp without time zone");

            entity.Property(e => e.ServerIv).HasColumnName("ServerIV");

            entity.Property(e => e.X25519publicKey).HasColumnName("X25519PublicKey");
         });

         modelBuilder.Entity<SchemaScaffold>(entity =>
         {
            entity.HasNoKey();

            entity.ToTable("Schema");

            entity.Property(e => e.Updated).HasColumnType("timestamp without time zone");
         });

         modelBuilder.Entity<UserScaffold>(entity =>
         {
            entity.ToTable("User");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.Property(e => e.Created).HasColumnType("timestamp without time zone");

            entity.Property(e => e.LastLogin).HasColumnType("timestamp without time zone");

            entity.ToSqlQuery(@"CREATE UNIQUE INDEX IF NOT EXISTS user_username_unique
ON public.""User"" USING btree
(LOWER(""Username"") COLLATE pg_catalog.""default"" ASC NULLS LAST)
TABLESPACE pg_default;");

            entity.ToSqlQuery(@"CREATE UNIQUE INDEX IF NOT EXISTS user_email_unique
ON public.""User"" USING btree
(LOWER(""Email"") COLLATE pg_catalog.""default"" ASC NULLS LAST)
TABLESPACE pg_default;");
         });

         modelBuilder.Entity<UserContactScaffold>(entity =>
         {
            entity.ToTable("UserContact");

            entity.HasIndex(e => e.Contact, "IX_UserContact_Contact");

            entity.HasIndex(e => e.Owner, "IX_UserContact_Owner");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.ContactNavigation)
                   .WithMany(p => p.UserContactContactNavigations)
                   .HasForeignKey(d => d.Contact);

            entity.HasOne(d => d.OwnerNavigation)
                   .WithMany(p => p.UserContactOwnerNavigations)
                   .HasForeignKey(d => d.Owner);
         });

         modelBuilder.Entity<UserEd25519KeyPairScaffold>(entity =>
         {
            entity.ToTable("UserEd25519KeyPair");

            entity.HasIndex(e => e.Owner, "Idx_UserEd25519KeyPair_Owner");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.Property(e => e.ClientIv).HasColumnName("ClientIV");

            entity.Property(e => e.Created).HasColumnType("timestamp without time zone");

            entity.HasOne(d => d.OwnerNavigation)
                   .WithMany(p => p.UserEd25519KeyPairs)
                   .HasForeignKey(d => d.Owner);
         });

         modelBuilder.Entity<UserEmailVerificationScaffold>(entity =>
         {
            entity.HasKey(e => e.Owner);

            entity.ToTable("UserEmailVerification");

            entity.Property(e => e.Owner).ValueGeneratedNever();

            entity.Property(e => e.Created).HasColumnType("timestamp without time zone");

            entity.HasOne(d => d.OwnerNavigation)
                   .WithOne(p => p.UserEmailVerification)
                   .HasForeignKey<UserEmailVerificationScaffold>(d => d.Owner);
         });

         modelBuilder.Entity<UserNotificationSettingScaffold>(entity =>
         {
            entity.HasKey(e => e.Owner);

            entity.ToTable("UserNotificationSetting");

            entity.Property(e => e.Owner).ValueGeneratedNever();

            entity.HasOne(d => d.OwnerNavigation)
                   .WithOne(p => p.UserNotificationSetting)
                   .HasForeignKey<UserNotificationSettingScaffold>(d => d.Owner);
         });

         modelBuilder.Entity<UserPrivacySettingScaffold>(entity =>
         {
            entity.HasKey(e => e.Owner);

            entity.ToTable("UserPrivacySetting");

            entity.Property(e => e.Owner).ValueGeneratedNever();

            entity.HasOne(d => d.OwnerNavigation)
                   .WithOne(p => p.UserPrivacySetting)
                   .HasForeignKey<UserPrivacySettingScaffold>(d => d.Owner);
         });

         modelBuilder.Entity<UserProfileScaffold>(entity =>
         {
            entity.HasKey(e => e.Owner);

            entity.ToTable("UserProfile");

            entity.Property(e => e.Owner).ValueGeneratedNever();

            entity.HasOne(d => d.OwnerNavigation)
                   .WithOne(p => p.UserProfile)
                   .HasForeignKey<UserProfileScaffold>(d => d.Owner);
         });

         modelBuilder.Entity<UserTokenScaffold>(entity =>
         {
            entity.ToTable("UserToken");

            entity.HasIndex(e => e.Owner, "Idx_UserToken_Owner");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.Property(e => e.Created).HasColumnType("timestamp without time zone");

            entity.Property(e => e.Expiration).HasColumnType("timestamp without time zone");

            entity.HasOne(d => d.OwnerNavigation)
                   .WithMany(p => p.UserTokens)
                   .HasForeignKey(d => d.Owner);
         });

         modelBuilder.Entity<UserX25519KeyPairScaffold>(entity =>
         {
            entity.ToTable("UserX25519KeyPair");

            entity.HasIndex(e => e.Owner, "Idx_UserX25519KeyPair_Owner");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.Property(e => e.ClientIv).HasColumnName("ClientIV");

            entity.Property(e => e.Created).HasColumnType("timestamp without time zone");

            entity.HasOne(d => d.OwnerNavigation)
                   .WithMany(p => p.UserX25519keyPairs)
                   .HasForeignKey(d => d.Owner);
         });

         OnModelCreatingPartial(modelBuilder);
      }

      partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
   }
}

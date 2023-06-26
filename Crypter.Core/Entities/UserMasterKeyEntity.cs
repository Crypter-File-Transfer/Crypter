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
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Crypter.Core.Entities
{
   public class UserMasterKeyEntity
   {
      public Guid Owner { get; set; }
      public byte[] EncryptedKey { get; set; }
      public byte[] Nonce { get; set; }
      public byte[] RecoveryProof { get; set; }
      public DateTime Updated { get; set; }
      public DateTime Created { get; set; }

      public UserEntity User { get; set; }

      public UserMasterKeyEntity(Guid owner, byte[] encryptedKey, byte[] nonce, byte[] recoveryProof, DateTime updated, DateTime created)
      {
         Owner = owner;
         EncryptedKey = encryptedKey;
         Nonce = nonce;
         RecoveryProof = recoveryProof;
         Updated = updated;
         Created = created;
      }
   }

   public class UserMasterKeyEntityConfiguration : IEntityTypeConfiguration<UserMasterKeyEntity>
   {
      public void Configure(EntityTypeBuilder<UserMasterKeyEntity> builder)
      {
         builder.ToTable("UserMasterKey");

         builder.HasKey(x => x.Owner);

         builder.Property(x => x.EncryptedKey)
            .IsRequired();

         builder.Property(x => x.Nonce)
            .IsRequired();

         builder.HasOne(x => x.User)
            .WithOne(x => x.MasterKey)
            .HasForeignKey<UserMasterKeyEntity>(x => x.Owner)
            .OnDelete(DeleteBehavior.Cascade);
      }
   }
}

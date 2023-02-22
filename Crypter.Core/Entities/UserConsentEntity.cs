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

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;

namespace Crypter.Core.Entities
{
   /// <summary>
   /// 
   /// </summary>
   /// <remarks>
   /// Ideally this class would not have an 'Id', but this is required in order to use Entity Framework.
   /// EF does not track keyless entities; it cannot even perform INSERT operations on them.
   /// 
   /// The 'Id' property is configured as an "Identity" column in the ModelBuilder.
   /// The database engine will take care of assigning this value.
   /// </remarks>
   public class UserConsentEntity
   {
      public long Id { get; set; }
      public Guid Owner { get; set; }
      public ConsentType ConsentType { get; set; }
      public DateTime Timestamp { get; set; }

      public UserEntity User { get; set; }

      public UserConsentEntity(Guid owner, ConsentType consentType, DateTime timestamp, long id = 0)
      {
         Id = id;
         Owner = owner;
         ConsentType = consentType;
         Timestamp = timestamp;
      }
   }

   public enum ConsentType
   {
      TermsOfService,
      PrivacyPolicy,
      RecoveryKeyRisks
   }

   public class UserConsentEntityConfiguration : IEntityTypeConfiguration<UserConsentEntity>
   {
      public void Configure(EntityTypeBuilder<UserConsentEntity> builder)
      {
         builder.ToTable("UserConsent");

         builder.HasKey(x => x.Id);

         builder.Property(x => x.Id)
            .UseIdentityAlwaysColumn();

         builder.HasIndex(x => x.Owner);

         builder.HasOne(x => x.User)
            .WithMany(x => x.Consents)
            .HasForeignKey(x => x.Owner)
            .OnDelete(DeleteBehavior.Cascade);
      }
   }
}

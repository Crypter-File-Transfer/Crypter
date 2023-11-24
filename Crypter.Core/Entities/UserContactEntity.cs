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

namespace Crypter.Core.Entities;

public class UserContactEntity
{
    public Guid OwnerId { get; set; }
    public Guid ContactId { get; set; }

    public UserEntity Owner { get; set; }
    public UserEntity Contact { get; set; }

    public UserContactEntity(Guid ownerId, Guid contactId)
    {
        OwnerId = ownerId;
        ContactId = contactId;
    }
}

public class UserContactEntityConfiguration : IEntityTypeConfiguration<UserContactEntity>
{
    public void Configure(EntityTypeBuilder<UserContactEntity> builder)
    {
        builder.ToTable("UserContact");

        builder.HasKey(x => new { x.OwnerId, x.ContactId });

        builder.Property(x => x.OwnerId)
            .HasColumnName("Owner");

        builder.Property(x => x.ContactId)
            .HasColumnName("Contact");

        builder.HasOne(x => x.Owner)
            .WithMany(x => x.Contacts)
            .HasForeignKey(x => x.OwnerId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Contact)
            .WithMany(x => x.Contactors)
            .HasForeignKey(x => x.ContactId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}

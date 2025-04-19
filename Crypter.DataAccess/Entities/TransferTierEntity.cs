/*
 * Copyright (C) 2025 Crypter File Transfer
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
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Crypter.DataAccess.Entities;

public class TransferTierEntity
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public long MaximumUploadSize { get; set; }
    public long UserQuota { get; set; }
    public UserCategory? DefaultForUserCategory { get; set; }
}

public class TransferTierEntityConfiguration : IEntityTypeConfiguration<TransferTierEntity>
{
    public void Configure(EntityTypeBuilder<TransferTierEntity> builder)
    {
        builder.ToTable("TransferTier");
        
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Id)
            .UseIdentityAlwaysColumn();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(64);
        
        builder.Property(x => x.Description)
            .IsRequired(false)
            .HasDefaultValue(null)
            .HasMaxLength(256);
        
        builder.Property(x => x.DefaultForUserCategory)
            .IsRequired(false)
            .HasDefaultValue(null);

        builder.HasIndex(x => x.DefaultForUserCategory)
            .IsUnique();
    }
}

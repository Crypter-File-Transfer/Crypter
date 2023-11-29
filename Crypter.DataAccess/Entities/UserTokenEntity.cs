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

using Crypter.Common.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Crypter.DataAccess.Entities;

public class UserTokenEntity
{
    public Guid Id { get; set; }
    public Guid Owner { get; set; }
    public string Description { get; set; }
    public TokenType Type { get; set; }
    public DateTime Created { get; set; }
    public DateTime Expiration { get; set; }

    public UserEntity? User { get; set; }

    public UserTokenEntity(Guid id, Guid owner, string description, TokenType type, DateTime created,
        DateTime expiration)
    {
        Id = id;
        Owner = owner;
        Description = description;
        Type = type;
        Created = created;
        Expiration = expiration;
    }
}

public class UserTokenEntityConfiguration : IEntityTypeConfiguration<UserTokenEntity>
{
    public void Configure(EntityTypeBuilder<UserTokenEntity> builder)
    {
        builder.ToTable("UserToken");

        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.User)
            .WithMany(x => x.Tokens)
            .HasForeignKey(x => x.Owner)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}

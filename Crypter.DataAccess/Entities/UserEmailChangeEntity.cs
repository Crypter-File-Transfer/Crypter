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

using Crypter.Common.Primitives;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Crypter.DataAccess.Entities;

public class UserEmailChangeEntity
{
    public Guid Owner { get; set; }
    public string EmailAddress { get; set; }
    public Guid? Code { get; set; }
    public byte[]? VerificationKey { get; set; }
    public DateTime? VerificationSent { get; set; }
    public DateTime Created { get; set; }

    public UserEntity? User { get; set; }

    /// <summary>
    /// Please avoid using this.
    /// This is only intended to be used by Entity Framework Core.
    /// </summary>
    [Obsolete("Use the other constructor.")]
    public UserEmailChangeEntity(Guid owner, string emailAddress, Guid? code, byte[]? verificationKey, DateTime? verificationSent, DateTime created)
    {
        Owner = owner;
        EmailAddress = emailAddress;
        Code = code;
        VerificationKey = verificationKey;
        VerificationSent = verificationSent;
        Created = created;
    }
    
    public UserEmailChangeEntity(Guid owner, EmailAddress emailAddress, DateTime created)
    {
        Owner = owner;
        EmailAddress = emailAddress.Value;
        Code = null;
        VerificationKey = null;
        Created = created;
    }
}

public class UserEmailChangeEntityConfiguration : IEntityTypeConfiguration<UserEmailChangeEntity>
{
    public void Configure(EntityTypeBuilder<UserEmailChangeEntity> builder)
    {
        builder.ToTable("UserEmailChange");

        builder.HasKey(x => x.Owner);
        
        builder.HasIndex(x => x.EmailAddress)
            .IsUnique();
        
        builder.HasIndex(x => x.Code)
            .IsUnique();

        builder.Property(x => x.EmailAddress)
            .HasColumnType("citext");
        
        builder.HasOne(x => x.User)
            .WithOne(x => x.EmailChange)
            .HasForeignKey<UserEmailChangeEntity>(x => x.Owner)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

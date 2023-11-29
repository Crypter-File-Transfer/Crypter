﻿/*
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

using Crypter.DataAccess.Entities.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Crypter.DataAccess.Entities;

public class UserFileTransferEntity : IUserTransfer, IFileTransfer
{
    public Guid Id { get; set; }
    public long Size { get; set; }
    public byte[] PublicKey { get; set; }
    public byte[] KeyExchangeNonce { get; set; }
    public byte[] Proof { get; set; }
    public DateTime Created { get; set; }
    public DateTime Expiration { get; set; }

    // IUserTransfer
    public Guid? SenderId { get; set; }
    public Guid? RecipientId { get; set; }

    public UserEntity? Sender { get; set; }
    public UserEntity? Recipient { get; set; }

    // IFileTransfer
    public string FileName { get; set; }
    public string ContentType { get; set; }

    public UserFileTransferEntity(Guid id, long size, byte[] publicKey, byte[] keyExchangeNonce, byte[] proof,
        DateTime created, DateTime expiration, Guid? senderId, Guid? recipientId, string fileName, string contentType)
    {
        Id = id;
        Size = size;
        PublicKey = publicKey;
        KeyExchangeNonce = keyExchangeNonce;
        Proof = proof;
        Created = created;
        Expiration = expiration;
        SenderId = senderId;
        RecipientId = recipientId;
        FileName = fileName;
        ContentType = contentType;
    }
}

public class UserFileTransferEntityConfiguration : IEntityTypeConfiguration<UserFileTransferEntity>
{
    public void Configure(EntityTypeBuilder<UserFileTransferEntity> builder)
    {
        builder.ToTable("UserFileTransfer");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SenderId)
            .HasColumnName("Sender");

        builder.Property(x => x.RecipientId)
            .HasColumnName("Recipient");

        builder.Property(x => x.PublicKey)
            .IsRequired(false);

        builder.Property(x => x.Proof)
            .IsRequired();

        builder.Property(x => x.FileName)
            .IsRequired();

        builder.Property(x => x.ContentType)
            .IsRequired();
    }
}

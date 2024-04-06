/*
 * Copyright (C) 2024 Crypter File Transfer
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

using Crypter.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace Crypter.DataAccess;

public class DataContext : DbContext
{
    public const string SchemaName = "crypter";

    /// <summary>
    /// This constructor is used during migrations.
    /// </summary>
    /// <param name="options"></param>
    public DataContext(DbContextOptions<DataContext> options) : base(options)
    {
    }

    public DbSet<UserEntity> Users { get; init; } = null!;
    public DbSet<UserProfileEntity> UserProfiles { get; init; } = null!;
    public DbSet<UserKeyPairEntity> UserKeyPairs { get; init; } = null!;
    public DbSet<UserPrivacySettingEntity> UserPrivacySettings { get; init; } = null!;
    public DbSet<UserEmailVerificationEntity> UserEmailVerifications { get; init; } = null!;
    public DbSet<UserNotificationSettingEntity> UserNotificationSettings { get; init; } = null!;
    public DbSet<UserTokenEntity> UserTokens { get; init; } = null!;
    public DbSet<UserContactEntity> UserContacts { get; init; } = null!;
    public DbSet<AnonymousFileTransferEntity> AnonymousFileTransfers { get; init; } = null!;
    public DbSet<AnonymousMessageTransferEntity> AnonymousMessageTransfers { get; init; } = null!;
    public DbSet<UserFileTransferEntity> UserFileTransfers { get; init; } = null!;
    public DbSet<UserMessageTransferEntity> UserMessageTransfers { get; init; } = null!;
    public DbSet<UserFailedLoginEntity> UserFailedLoginAttempts { get; init; } = null!;
    public DbSet<UserMasterKeyEntity> UserMasterKeys { get; init; } = null!;
    public DbSet<UserConsentEntity> UserConsents { get; init; } = null!;
    public DbSet<UserRecoveryEntity> UserRecoveries { get; init; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasPostgresExtension("citext")
            .HasDefaultSchema(SchemaName)
            .ApplyConfigurationsFromAssembly(typeof(DataContext).Assembly);
    }
}

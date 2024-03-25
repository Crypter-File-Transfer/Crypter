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

    public DbSet<AnonymousFileTransferEntity> AnonymousFileTransfers { get; set; } = null!;
    public DbSet<AnonymousMessageTransferEntity> AnonymousMessageTransfers { get; set; } = null!;
    public DbSet<UserConsentEntity> UserConsents { get; set; } = null!;
    public DbSet<UserContactEntity> UserContacts { get; set; } = null!;
    public DbSet<UserEmailVerificationEntity> UserEmailVerifications { get; set; } = null!;
    public DbSet<UserEntity> Users { get; set; } = null!;
    public DbSet<UserFailedLoginEntity> UserFailedLoginAttempts { get; set; } = null!;
    public DbSet<UserFileTransferEntity> UserFileTransfers { get; set; } = null!;
    public DbSet<UserKeyPairEntity> UserKeyPairs { get; set; } = null!;
    public DbSet<UserMasterKeyEntity> UserMasterKeys { get; set; } = null!;
    public DbSet<UserMessageTransferEntity> UserMessageTransfers { get; set; } = null!;
    public DbSet<UserNotificationSettingEntity> UserNotificationSettings { get; set; } = null!;
    public DbSet<UserPrivacySettingEntity> UserPrivacySettings { get; set; } = null!;
    public DbSet<UserProfileEntity> UserProfiles { get; set; } = null!;
    public DbSet<UserRecoveryEntity> UserRecoveries { get; set; } = null!;
    public DbSet<UserTokenEntity> UserTokens { get; set; } = null!;
    
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        
        configurationBuilder.Properties<Enum>()
            .HaveConversion<string>();
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.HasPostgresExtension("citext")
            .HasDefaultSchema(SchemaName)
            .ApplyConfigurationsFromAssembly(typeof(DataContext).Assembly);
    }
}

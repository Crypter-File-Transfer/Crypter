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

using Crypter.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Crypter.Core
{
   public class DataContext : DbContext
   {
      protected readonly IConfiguration Configuration;

      public DataContext(IConfiguration configuration)
      {
         Configuration = configuration;
      }

      protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
      {
         var connectionString = Configuration.GetConnectionString("DefaultConnection");
         optionsBuilder.UseNpgsql(connectionString);
      }

      public DbSet<User> Users { get; set; }
      public DbSet<UserProfile> UserProfiles { get; set; }
      public DbSet<UserX25519KeyPair> UserX25519KeyPairs { get; set; }
      public DbSet<UserEd25519KeyPair> UserEd25519KeyPairs { get; set; }
      public DbSet<UserPrivacySetting> UserPrivacySettings { get; set; }
      public DbSet<UserEmailVerification> UserEmailVerifications { get; set; }
      public DbSet<UserNotificationSetting> UserNotificationSettings { get; set; }
      public DbSet<UserToken> UserTokens { get; set; }
      public DbSet<FileTransfer> FileTransfers { get; set; }
      public DbSet<MessageTransfer> MessageTransfers { get; set; }
      public DbSet<Schema> Schema { get; set; }
   }
}

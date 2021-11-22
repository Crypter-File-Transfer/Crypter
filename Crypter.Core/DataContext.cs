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

      public DbSet<User> User { get; set; }
      public DbSet<UserProfile> UserProfile { get; set; }
      public DbSet<UserX25519KeyPair> UserX25519KeyPair { get; set; }
      public DbSet<UserEd25519KeyPair> UserEd25519KeyPair { get; set; }
      public DbSet<UserPrivacySetting> UserPrivacySetting { get; set; }
      public DbSet<UserEmailVerification> UserEmailVerification { get; set; }
      public DbSet<UserNotificationSetting> UserNotificationSetting { get; set; }
      public DbSet<FileTransfer> FileTransfer { get; set; }
      public DbSet<MessageTransfer> MessageTransfer { get; set; }
      public DbSet<Schema> Schema { get; set; }
   }
}

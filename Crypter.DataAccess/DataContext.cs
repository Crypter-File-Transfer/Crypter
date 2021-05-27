using Crypter.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Crypter.DataAccess
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
            var serverVersion = ServerVersion.AutoDetect(connectionString);
            optionsBuilder.UseMySql(connectionString, serverVersion);
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Key> Keys { get; set; }
        public DbSet<FileItem> Files { get; set; }
        public DbSet<MessageItem> Messages { get; set; }
        public DbSet<BetaKey> BetaKeys { get; set; }
    }
}

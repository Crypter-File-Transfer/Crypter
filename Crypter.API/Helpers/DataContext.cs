using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Crypter.DataAccess.Models;
using Crypter.DataAccess.DTO; 

namespace Crypter.API.Helpers
{
    public class DataContext : DbContext
    {
        protected readonly IConfiguration Configuration;

        public DataContext(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            // connect to sql server database
            options.UseMySQL(Configuration.GetConnectionString("DefaultConnection"));
        }

        public DbSet<User> Users { get; set; }
        public DbSet<UserFileUploadDTO> FileUploads { get; set; }
        public DbSet<UserMessageUploadDTO> MessageUploads { get; set; }
        public DbSet<Key> Keys { get; set; }
    }
}

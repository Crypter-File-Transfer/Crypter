using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Crypter.DataAccess.Models; 

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
            options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"));
        }

        public DbSet<User> Users { get; set; }
    }
}

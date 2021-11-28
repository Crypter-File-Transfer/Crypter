using Crypter.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Crypter.Core.Services.DataAccess
{
   public class SchemaService : ISchemaService
   {
      private readonly DataContext Context;

      public SchemaService(DataContext context)
      {
         Context = context;
      }

      public async Task<ISchema> ReadAsync()
      {
         return await Context.Schema.FirstOrDefaultAsync();
      }
   }
}

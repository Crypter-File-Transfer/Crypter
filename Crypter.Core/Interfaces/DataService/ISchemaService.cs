using System.Threading.Tasks;

namespace Crypter.Core.Interfaces
{
   public interface ISchemaService
   {
      Task<ISchema> ReadAsync();
   }
}

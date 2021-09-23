using System.ComponentModel.DataAnnotations.Schema;

namespace Crypter.Core.Models
{
   [Table("BetaKey")]
   public class BetaKey
   {
      public int Id { get; set; }
      public string Key { get; set; }
   }
}

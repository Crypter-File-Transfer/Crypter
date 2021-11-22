using Crypter.Core.Interfaces;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Crypter.Core.Models
{
   [Table("Schema")]
   public class Schema : ISchema
   {
      public int Version { get; set; }
      public DateTime Updated { get; set; }

      public Schema(int version, DateTime updated)
      {
         Version = version;
         Updated = updated;
      }
   }
}

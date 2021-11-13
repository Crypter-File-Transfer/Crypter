using Crypter.Core.Interfaces;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Crypter.Core.Models
{
   [Table("UserEmailVerification")]
   public class UserEmailVerification : IUserEmailVerification
   {
      [Key]
      [ForeignKey("User")]
      public Guid Owner { get; set; }
      public Guid Code { get; set; }
      public byte[] VerificationKey { get; set; }
      public DateTime Created { get; set; }

      public virtual User User { get; set; }

      public UserEmailVerification(Guid owner, Guid code, byte[] verificationKey, DateTime created)
      {
         Owner = owner;
         Code = code;
         VerificationKey = verificationKey;
         Created = created;
      }
   }
}

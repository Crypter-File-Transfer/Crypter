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
      public Guid Owner { get; set; }
      public string VerificationCode { get; set; }
      public DateTime Created { get; set; }

      public UserEmailVerification(Guid owner, string verificationCode, DateTime created)
      {
         Owner = owner;
         VerificationCode = verificationCode;
         Created = created;
      }
   }
}

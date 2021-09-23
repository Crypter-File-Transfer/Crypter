using Crypter.Core.Interfaces;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Crypter.Core.Models
{
   [Table("UserProfile")]
   public class UserProfile : IUserProfile
   {
      [Key]
      [ForeignKey("User")]
      public Guid Owner { get; set; }
      public string Alias { get; set; }
      public string About { get; set; }
      public string Image { get; set; }

      public virtual User User { get; set; }

      public UserProfile(Guid owner, string alias, string about, string image)
      {
         Owner = owner;
         Alias = alias;
         About = about;
         Image = image;
      }
   }
}

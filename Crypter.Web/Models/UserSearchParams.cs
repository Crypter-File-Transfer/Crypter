using System.ComponentModel.DataAnnotations;

namespace Crypter.Web.Models
{
   public class UserSearchParams
   {
      [Required]
      public string Type { get; set; }

      [Required]
      [Display(Name = "Search term")]
      [MinLength(1, ErrorMessage = "Search term cannot be empty")]
      public string Query { get; set; }

      public int Index { get; set; }

      public int Results { get; set; }

      public int Page { get; set; }

      public UserSearchParams()
      {
         Type = "username";
         Index = 0;
         Results = 20;
         Page = 1;
      }
   }
}

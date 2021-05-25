using System.ComponentModel.DataAnnotations;

namespace Crypter.Web.Models
{
    public class SearchInfo
    {
        [Required]
        public string Type { get; set; }

        [Required]
        [Display(Name = "Search Term")]
        [MinLength(2, ErrorMessage = "Search Term must be at least 2 characters")]
        public string Query { get; set; }

        public int Index { get; set; }

        public int NumResults { get; set; }

        public int Page { get; set; }

        public SearchInfo()
        {
            Type = "username";
            Index = 0;
            NumResults = 20;
            Page = 1;
        }
    }
}

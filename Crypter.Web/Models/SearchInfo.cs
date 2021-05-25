using System.ComponentModel.DataAnnotations;

namespace Crypter.Web.Models
{
    public class SearchInfo
    {
        [Required]
        public string Type { get; set; }

        [Required]
        public string Query { get; set; }

        public int Index { get; set; }

        public int NumResults { get; set; }

        public int Page { get; set; }

        public SearchInfo()
        {
            Index = 0;
            NumResults = 20;
            Page = 1;
        }
    }
}

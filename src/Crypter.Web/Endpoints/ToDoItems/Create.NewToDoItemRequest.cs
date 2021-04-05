using System.ComponentModel.DataAnnotations;

namespace Crypter.Web.Endpoints.ToDoItems
{
    public class NewToDoItemRequest
    {
        [Required]
        public string Title { get; set; }
        public string Description { get; set; }
    }
}
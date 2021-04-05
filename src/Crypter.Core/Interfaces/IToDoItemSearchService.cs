using System.Collections.Generic;
using System.Threading.Tasks;
using Ardalis.Result;
using Crypter.Core.Entities;

namespace Crypter.Core.Interfaces
{
    public interface IToDoItemSearchService
    {
        Task<Result<ToDoItem>> GetNextIncompleteItemAsync();
        Task<Result<List<ToDoItem>>> GetAllIncompleteItemsAsync(string searchString);
    }
}

using Crypter.Common.Contracts.Features.Users;
using Crypter.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.API.Controllers
{
   [ApiController]
   [Route("api/search")]
   public class SearchController : CrypterController
   {
      private readonly IUserService _userService;
      private readonly ITokenService _tokenService;

      public SearchController(IUserService userService, ITokenService tokenService)
      {
         _userService = userService;
         _tokenService = tokenService;
      }

      [HttpGet("user")]
      [Authorize]
      [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UserSearchResponse))]
      [ProducesResponseType(StatusCodes.Status401Unauthorized, Type = typeof(void))]
      public async Task<IActionResult> SearchUsersAsync([FromQuery] string value, [FromQuery] int index, [FromQuery] int count, CancellationToken cancellationToken)
      {
         var userId = _tokenService.ParseUserId(User);
         var result = await _userService.SearchForUsersAsync(userId, value, index, count, cancellationToken);
         return Ok(result);
      }
   }
}

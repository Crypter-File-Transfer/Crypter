using Microsoft.AspNetCore.Mvc;

namespace Crypter.Web.Api
{
    [Route("api/[controller]")]
    [ApiController]
    public abstract class BaseApiController : Controller
    {
    }
}

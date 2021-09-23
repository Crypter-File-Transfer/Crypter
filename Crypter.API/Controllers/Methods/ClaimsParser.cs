using System;
using System.Linq;
using System.Security.Claims;

namespace Crypter.API.Controllers.Methods
{
   public class ClaimsParser
   {
      public static Guid ParseUserId(ClaimsPrincipal user)
      {
         var userClaim = user.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier);
         return userClaim is null
            ? Guid.Empty
            : Guid.Parse(userClaim.Value);
      }
   }
}

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
         if (userClaim is null)
         {
            return Guid.Empty;
         }

         if (!Guid.TryParse(userClaim.Value, out Guid userId))
         {
            return Guid.Empty;
         }

         return userId;
      }
   }
}

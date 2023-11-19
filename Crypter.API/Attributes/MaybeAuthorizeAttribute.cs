/*
 * Copyright (C) 2023 Crypter File Transfer
 * 
 * This file is part of the Crypter file transfer project.
 * 
 * Crypter is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * The Crypter source code is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 * 
 * You should have received a copy of the GNU Affero General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 * 
 * You can be released from the requirements of the aforementioned license
 * by purchasing a commercial license. Buying such a license is mandatory
 * as soon as you develop commercial activities involving the Crypter source
 * code without disclosing the source code of your own applications.
 * 
 * Contact the current copyright holder to discuss commercial license options.
 */

using System;
using System.Security.Claims;
using Crypter.Core.Services;
using EasyMonads;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Crypter.API.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
internal class MaybeAuthorizeAttribute : Attribute, IAuthorizationFilter
{
   public void OnAuthorization(AuthorizationFilterContext context)
   {
      string authorization = context.HttpContext.Request.Headers.Authorization;
      bool authorizationProvided = !string.IsNullOrEmpty(authorization);

      if (authorizationProvided)
      {
         string[] authorizationParts = authorization.Split(' ');
         if (authorizationParts.Length != 2 || authorizationParts[0].ToLower() != "bearer")
         {
            context.Result = new UnauthorizedResult();
            return;
         }

         ITokenService tokenService = (ITokenService)context.HttpContext.RequestServices.GetService(typeof(ITokenService));
         Maybe<ClaimsPrincipal> maybeClaims = tokenService.ValidateToken(authorizationParts[1]);

         maybeClaims.IfSome(x => context.HttpContext.User = x);
         maybeClaims.IfNone(() => context.Result = new UnauthorizedResult());
      }
   }
}
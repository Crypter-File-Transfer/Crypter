/*
 * Copyright (C) 2021 Crypter File Transfer
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
 * Contact the current copyright holder to discuss commerical license options.
 */

using Crypter.API.Controllers.Methods;
using NUnit.Framework;
using System;
using System.Security.Claims;

namespace Crypter.Test.API_Tests
{
   [TestFixture]
   public class ClaimsParser_Tests
   {
      [SetUp]
      public void Setup()
      {
      }

      [Test]
      public void ParseUserId_CanParse_ReturnsGuid()
      {
         var knownUserId = new Guid("7c071eaa-dc8d-460b-845d-c9b9842b432b");
         var claim = new Claim(ClaimTypes.NameIdentifier, knownUserId.ToString());

         var identity = new ClaimsIdentity();
         identity.AddClaim(claim);

         var claimsPrincipal = new ClaimsPrincipal();
         claimsPrincipal.AddIdentity(identity);

         var foundUserId = ClaimsParser.ParseUserId(claimsPrincipal);
         Assert.AreEqual(knownUserId, foundUserId);
      }

      [Test]
      public void ParseUserId_NoMatchingClaims_ReturnsEmptyGuid()
      {
         var knownUserId = new Guid("7c071eaa-dc8d-460b-845d-c9b9842b432b");
         var claim = new Claim(ClaimTypes.Name, knownUserId.ToString());  // Our ClaimsParser does not care about ClaimTypes.Name

         var identity = new ClaimsIdentity();
         identity.AddClaim(claim);

         var claimsPrincipal = new ClaimsPrincipal();
         claimsPrincipal.AddIdentity(identity);

         var emptyGuid = ClaimsParser.ParseUserId(claimsPrincipal);
         Assert.AreEqual(Guid.Empty, emptyGuid);
      }

      [Test]
      public void ParseUserId_NoClaims_ReturnsEmptyGuid()
      {
         var identity = new ClaimsIdentity();

         var claimsPrincipal = new ClaimsPrincipal();
         claimsPrincipal.AddIdentity(identity);

         var emptyGuid = ClaimsParser.ParseUserId(claimsPrincipal);
         Assert.AreEqual(Guid.Empty, emptyGuid);
      }

      [Test]
      public void ParseUserId_InvalidClaimValue_ReturnsEmptyGuid()
      {
         var knownUserId = "foo";
         var claim = new Claim(ClaimTypes.NameIdentifier, knownUserId.ToString());

         var identity = new ClaimsIdentity();
         identity.AddClaim(claim);

         var claimsPrincipal = new ClaimsPrincipal();
         claimsPrincipal.AddIdentity(identity);

         var emptyGuid = ClaimsParser.ParseUserId(claimsPrincipal);
         Assert.AreEqual(Guid.Empty, emptyGuid);
      }
   }
}

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

/*
 * Copyright (C) 2022 Crypter File Transfer
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

using Crypter.Common.Enums;
using Crypter.Common.Models;
using Crypter.Common.Primitives;
using Crypter.Contracts.Features.Authentication;
using Crypter.Core.Entities;
using Crypter.Core.Models;
using Crypter.Core.Services;
using Crypter.Test.Core_Tests.Services_Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Crypter.Test.Core_Tests.Services_Tests
{
   [TestFixture]
   public class UserAuthenticationService_Tests
   {
      private TestDataContext _testContext;
      private PasswordHashService _passwordHashService;

      [OneTimeSetUp]
      public void SetupOnce()
      {
         _testContext = new TestDataContext(GetType().Name);
         _testContext.EnsureCreated();
         _passwordHashService = ServiceTestFactory.GetPasswordHashService();
      }

      [TearDown]
      public void Teardown()
      {
         _testContext.Reset();
      }

      [Test]
      public async Task Login_Request_Validation_Requires_Valid_Password_Version()
      {
         PasswordVersion passwordVersion = new PasswordVersion
         {
            Version = 0,
            Algorithm = "foo",
            Iterations = 1
         };

         ServerPasswordSettings passwordSettings = new ServerPasswordSettings
         {
            ClientVersion = 5,
            ServerVersions = new List<PasswordVersion> { passwordVersion }
         };

         var sut = ServiceTestFactory.GetUserAuthenticationService(_testContext, passwordSettings);

         VersionedPassword userPassword = new VersionedPassword("password", 0);
         LoginRequest request = new LoginRequest(Username.From("jack"), new List<VersionedPassword> { userPassword }, TokenType.Session);

         var result = await sut.LoginAsync(request, "test", CancellationToken.None);
         Assert.True(result.IsLeft);
         result.DoLeftOrNeither(
            left =>
            {
               Assert.AreEqual(LoginError.InvalidPasswordVersion, left);
            },
            () => Assert.Fail());
      }

      [Test]
      public async Task Login_Request_Validation_Requires_Valid_Username()
      {
         var sut = ServiceTestFactory.GetUserAuthenticationService(_testContext);

         VersionedPassword userPassword = new VersionedPassword("password", 0);
         LoginRequest request = new LoginRequest(string.Empty, new List<VersionedPassword> { userPassword }, TokenType.Session);

         var result = await sut.LoginAsync(request, "test", CancellationToken.None);
         Assert.True(result.IsLeft);
         result.DoLeftOrNeither(
            left =>
            {
               Assert.AreEqual(LoginError.InvalidUsername, left);
            },
            () => Assert.Fail());
      }

      [Test]
      public async Task Login_Request_Validation_Requires_Valid_Password()
      {
         var sut = ServiceTestFactory.GetUserAuthenticationService(_testContext);

         VersionedPassword userPassword = new VersionedPassword(string.Empty, 0);
         LoginRequest request = new LoginRequest("jack", new List<VersionedPassword> { userPassword }, TokenType.Session);

         var result = await sut.LoginAsync(request, "test", CancellationToken.None);
         Assert.True(result.IsLeft);
         result.DoLeftOrNeither(
            left =>
            {
               Assert.AreEqual(LoginError.InvalidPassword, left);
            },
            () => Assert.Fail());
      }

      [Test]
      public async Task Login_Request_Validation_Requires_Valid_Refresh_Token_Type()
      {
         var sut = ServiceTestFactory.GetUserAuthenticationService(_testContext);

         VersionedPassword userPassword = new VersionedPassword("foo", 0);
         LoginRequest request = new LoginRequest("jack", new List<VersionedPassword> { userPassword }, TokenType.Authentication);

         var result = await sut.LoginAsync(request, "test", CancellationToken.None);
         Assert.True(result.IsLeft);
         result.DoLeftOrNeither(
            left =>
            {
               Assert.AreEqual(LoginError.InvalidTokenTypeRequested, left);
            },
            () => Assert.Fail());
      }

      [Test]
      public async Task Login_Fails_If_User_Does_Not_Exist()
      {
         var sut = ServiceTestFactory.GetUserAuthenticationService(_testContext);

         VersionedPassword userPassword = new VersionedPassword("foo", 0);
         LoginRequest request = new LoginRequest("jack", new List<VersionedPassword> { userPassword }, TokenType.Session);

         var result = await sut.LoginAsync(request, "test", CancellationToken.None);
         Assert.True(result.IsLeft);
         result.DoLeftOrNeither(
            left =>
            {
               Assert.AreEqual(LoginError.InvalidUsername, left);
            },
            () => Assert.Fail());
      }

      [Test]
      public async Task Login_Fails_If_User_Has_Too_Many_Failed_Login_Attempts()
      {
         string username = "jack";
         var now = DateTime.UtcNow;
         UserEntity user = new UserEntity(Guid.NewGuid(), username, string.Empty, new byte[] { 0x00 }, new byte[] { 0x00 }, 0, 0, false, now, now);
         user.FailedLoginAttempts = new List<UserFailedLoginEntity>
         {
            new UserFailedLoginEntity(Guid.NewGuid(), user.Id, now),
            new UserFailedLoginEntity(Guid.NewGuid(), user.Id, now),
            new UserFailedLoginEntity(Guid.NewGuid(), user.Id, now)
         };
         _testContext.Add(user);
         await _testContext.SaveChangesAsync();

         var sut = ServiceTestFactory.GetUserAuthenticationService(_testContext);

         VersionedPassword userPassword = new VersionedPassword("foo", 0);
         LoginRequest request = new LoginRequest(username, new List<VersionedPassword> { userPassword }, TokenType.Session);

         var result = await sut.LoginAsync(request, "test", CancellationToken.None);
         Assert.True(result.IsLeft);
         result.DoLeftOrNeither(
            left =>
            {
               Assert.AreEqual(LoginError.ExcessiveFailedLoginAttempts, left);
            },
            () => Assert.Fail());
      }

      [Test]
      public async Task Login_Records_Login_Failure_If_Password_Does_Not_Match()
      {
         string username = "jack";
         var now = DateTime.UtcNow;
         AuthenticationPassword userPassword = AuthenticationPassword.From("foo");
         var hashOutput = _passwordHashService.MakeSecurePasswordHash(userPassword, 1);
         UserEntity user = new UserEntity(Guid.NewGuid(), username, string.Empty, hashOutput.Hash, hashOutput.Salt, 0, 0, false, now, now);
         _testContext.Add(user);
         await _testContext.SaveChangesAsync();

         var sut = ServiceTestFactory.GetUserAuthenticationService(_testContext);

         VersionedPassword providedUserPassword = new VersionedPassword("not-foo", 0);
         LoginRequest request = new LoginRequest(username, new List<VersionedPassword> { providedUserPassword }, TokenType.Session);

         var result = await sut.LoginAsync(request, "test", CancellationToken.None);
         Assert.True(result.IsLeft);
         result.DoLeftOrNeither(
            left =>
            {
               Assert.AreEqual(LoginError.InvalidPassword, left);
            },
            () => Assert.Fail());

         int userFailedLoginAttempts = await _testContext.UserFailedLoginAttempts
            .CountAsync(x => x.Owner == user.Id);

         Assert.AreEqual(1, userFailedLoginAttempts);
      }

      [TestCase(0, 0)]
      [TestCase(0, 1)]
      [TestCase(1, 0)]
      [TestCase(1, 1)]
      public async Task Login_Succeeds_And_Does_Not_Migrate_Password_When_All_Password_Versions_Match(int serverPasswordVersion, int clientPasswordVersion)
      {
         string username = "jack";
         string password = "foo";
         var now = DateTime.UtcNow;
         AuthenticationPassword userPassword = AuthenticationPassword.From(password);

         ServerPasswordSettings passwordSettings = ServiceTestFactory.GetPasswordSettings(serverPasswordVersion, clientPasswordVersion);
         var sut = ServiceTestFactory.GetUserAuthenticationService(_testContext, passwordSettings);

         var hashOutput = _passwordHashService.MakeSecurePasswordHash(userPassword, passwordSettings.ServerVersions[0].Iterations);
         UserEntity user = new UserEntity(Guid.NewGuid(), username, string.Empty, hashOutput.Hash, hashOutput.Salt, serverPasswordVersion, clientPasswordVersion, false, now, now);
         _testContext.Add(user);
         await _testContext.SaveChangesAsync();

         VersionedPassword providedUserPassword = new VersionedPassword(password, clientPasswordVersion);
         LoginRequest request = new LoginRequest(username, new List<VersionedPassword> { providedUserPassword }, TokenType.Session);

         var result = await sut.LoginAsync(request, "test", CancellationToken.None);
         Assert.True(result.IsRight);

         var fetchedUser = await _testContext.Users
            .FirstAsync(x => x.Id == user.Id);

         Assert.AreEqual(serverPasswordVersion, fetchedUser.ServerPasswordVersion);
         Assert.AreEqual(clientPasswordVersion, fetchedUser.ClientPasswordVersion);
         Assert.AreEqual(hashOutput.Hash, fetchedUser.PasswordHash);
         Assert.AreEqual(hashOutput.Salt, fetchedUser.PasswordSalt);
      }

      [Test]
      public async Task Login_Succeeds_And_Migrates_Password_When_Server_Password_Version_Is_Greater()
      {
         string username = "jack";
         string password = "foo";
         var now = DateTime.UtcNow;
         AuthenticationPassword userPassword = AuthenticationPassword.From(password);

         ServerPasswordSettings passwordSettings = new ServerPasswordSettings
         {
            ClientVersion = 0,
            ServerVersions = new List<PasswordVersion>
            {
               new PasswordVersion { Algorithm = "x", Version = 0, Iterations = 1 },
               new PasswordVersion { Algorithm = "x", Version = 1, Iterations = 2 }
            }
         };
         var sut = ServiceTestFactory.GetUserAuthenticationService(_testContext, passwordSettings);

         var hashOutput = _passwordHashService.MakeSecurePasswordHash(userPassword, passwordSettings.ServerVersions[0].Iterations);
         UserEntity user = new UserEntity(Guid.NewGuid(), username, string.Empty, hashOutput.Hash, hashOutput.Salt, 0, 0, false, now, now);
         _testContext.Add(user);
         await _testContext.SaveChangesAsync();

         VersionedPassword providedUserPassword = new VersionedPassword(password, 0);
         LoginRequest request = new LoginRequest(username, new List<VersionedPassword> { providedUserPassword }, TokenType.Session);

         var preMigrationLoginResult = await sut.LoginAsync(request, "test", CancellationToken.None);
         Assert.True(preMigrationLoginResult.IsRight);

         var fetchedUser = await _testContext.Users
            .FirstAsync(x => x.Id == user.Id);

         Assert.AreEqual(1, fetchedUser.ServerPasswordVersion);
         Assert.AreEqual(0, fetchedUser.ClientPasswordVersion);
         Assert.AreNotEqual(hashOutput.Hash, fetchedUser.PasswordHash);
         Assert.AreNotEqual(hashOutput.Salt, fetchedUser.PasswordSalt);

         // Verify the user can still login after the migration
         var postMigrationLogiResult = await sut.LoginAsync(request, "test", CancellationToken.None);
         Assert.True(postMigrationLogiResult.IsRight);
      }

      [Test]
      public async Task Login_Succeeds_And_Migrates_Password_When_Client_Password_Version_Is_Greater()
      {
         string username = "jack";
         string originalClientPassword = "foo";
         string newClientPassword = "bar";
         var now = DateTime.UtcNow;

         AuthenticationPassword originalAuthPassword = AuthenticationPassword.From(originalClientPassword);
         AuthenticationPassword newAuthPassword = AuthenticationPassword.From(newClientPassword);

         ServerPasswordSettings serverPasswordSettings = ServiceTestFactory.GetPasswordSettings(0, 1);
         var sut = ServiceTestFactory.GetUserAuthenticationService(_testContext, serverPasswordSettings);

         var originalHashOutput = _passwordHashService.MakeSecurePasswordHash(originalAuthPassword, serverPasswordSettings.ServerVersions[0].Iterations);
         UserEntity user = new UserEntity(Guid.NewGuid(), username, string.Empty, originalHashOutput.Hash, originalHashOutput.Salt, 0, 0, false, now, now);
         _testContext.Add(user);
         await _testContext.SaveChangesAsync();

         // The first login attempt should fail, because the provided client password version is too high
         VersionedPassword newVersionedPassword = new VersionedPassword(newClientPassword, 1);
         List<VersionedPassword> providedUserPasswords = new List<VersionedPassword>
         {
            newVersionedPassword
         };
         LoginRequest loginRequestWithoutOriginalPassword = new LoginRequest(username, providedUserPasswords, TokenType.Session);
         var loginResultWithoutOriginalPassword = await sut.LoginAsync(loginRequestWithoutOriginalPassword, "test", CancellationToken.None);

         Assert.True(loginResultWithoutOriginalPassword.IsLeft);
         loginResultWithoutOriginalPassword.DoLeftOrNeither(
            error => Assert.AreEqual(LoginError.InvalidPasswordVersion, error),
            () => Assert.Fail());

         // The second login attempt should succeed, because both client password versions are provided
         VersionedPassword originalVersionedPassword = new VersionedPassword(originalAuthPassword, 0);
         providedUserPasswords.Add(originalVersionedPassword);
         LoginRequest loginRequestWithOriginalPassword = new LoginRequest(username, providedUserPasswords, TokenType.Session);
         var loginResultWithOriginalPassword = await sut.LoginAsync(loginRequestWithOriginalPassword, "test", CancellationToken.None);

         var fetchedUser = await _testContext.Users
            .FirstAsync(x => x.Id == user.Id);

         Assert.True(loginResultWithOriginalPassword.IsRight);
         Assert.AreEqual(0, fetchedUser.ServerPasswordVersion);
         Assert.AreEqual(1, fetchedUser.ClientPasswordVersion);
         Assert.AreNotEqual(originalHashOutput.Hash, fetchedUser.PasswordHash);
         Assert.AreNotEqual(originalHashOutput.Salt, fetchedUser.PasswordSalt);

         // Verify the user can still login after the migration
         providedUserPasswords.Remove(originalVersionedPassword);
         LoginRequest newLoginRequest = new LoginRequest(username, providedUserPasswords, TokenType.Session);
         var postMigrationLogiResult = await sut.LoginAsync(newLoginRequest, "test", CancellationToken.None);
         Assert.True(postMigrationLogiResult.IsRight);
      }
   }
}

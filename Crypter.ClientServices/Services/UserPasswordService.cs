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

using Crypter.ClientServices.Interfaces;
using Crypter.ClientServices.Interfaces.Enum;
using Crypter.ClientServices.Interfaces.Events;
using Crypter.Common.Contracts.Features.Authentication;
using Crypter.Common.Monads;
using Crypter.Common.Primitives;
using Crypter.Crypto.Common;
using Crypter.Crypto.Common.PasswordHash;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crypter.ClientServices.Services
{
   public class UserPasswordService : IUserPasswordService
   {
      private const int _currentPasswordVersion = 1;
      private const int _credentialKeySize = 32;
      private const int _authenticationPasswordSize = 64;

      private readonly ICryptoProvider _cryptoProvider;

      private EventHandler<PasswordHashBeginEventArgs> _passwordHashBeginEventHandler;
      private EventHandler<PasswordHashEndEventArgs> _passwordHashEndEventHandler;

      public int CurrentPasswordVersion
      { get { return _currentPasswordVersion; } }

      public int CredentialKeySize
      { get { return _credentialKeySize; } }

      public int AuthenticationPasswordSize
      { get { return _authenticationPasswordSize; } }

      public UserPasswordService(ICryptoProvider cryptoProvider)
      {
         _cryptoProvider = cryptoProvider;
      }

      public Task<Maybe<byte[]>> DeriveUserCredentialKeyAsync(Username username, Password password, int passwordVersion)
      {
         return passwordVersion switch
         {
            0 => throw new NotImplementedException(),
            1 => DeriveArgonCredentialKeyAsync(username, password),
            _ => throw new NotImplementedException()
         };
      }

      public async Task<Maybe<VersionedPassword>> DeriveUserAuthenticationPasswordAsync(Username username, Password password, int passwordVersion)
      {
#pragma warning disable CS0618
         return passwordVersion switch
         {
            0 => DeriveSha512AuthenticationPassword(username, password),
            1 => await DeriveArgonAuthenticationPasswordAsync(username, password),
            _ => throw new NotImplementedException()
         };
#pragma warning restore CS0618
      }

      [Obsolete("Use DeriveArgonAuthenticationPassword instead")]
      public VersionedPassword DeriveSha512AuthenticationPassword(Username username, Password password)
      {
         byte[] passwordBytes = Encoding.UTF8.GetBytes(password.Value);
         byte[] usernameBytes = Encoding.UTF8.GetBytes(username.Value.ToLower());
         IEnumerable<byte> seedData = passwordBytes.Concat(usernameBytes);

         byte[] hashedPassword = _cryptoProvider.CryptoHash.GenerateSha512(seedData.ToArray());
         return new VersionedPassword(hashedPassword, 0);
      }

      public async Task<Maybe<VersionedPassword>> DeriveArgonAuthenticationPasswordAsync(Username username, Password password)
      {
         OnPasswordHashBeginEvent(PasswordHashType.AuthenticationKey);
         await Task.Delay(1);

         uint hashKeySize = _cryptoProvider.GenericHash.KeySize;
         byte[] hashedUsername = _cryptoProvider.GenericHash.GenerateHash(hashKeySize, username.Value.ToLower());

         uint saltSize = _cryptoProvider.PasswordHash.SaltSize;
         byte[] salt = _cryptoProvider.GenericHash.GenerateHash(saltSize, password.Value, hashedUsername);

         Maybe<VersionedPassword> hashResult = _cryptoProvider.PasswordHash.GenerateKey(password.Value, salt, _authenticationPasswordSize, OpsLimit.Sensitive, MemLimit.Moderate)
            .Map(x => new VersionedPassword(x, 1))
            .ToMaybe();

         OnPasswordHashEndEvent(hashResult.IsSome);
         return hashResult;
      }

      public async Task<Maybe<byte[]>> DeriveArgonCredentialKeyAsync(Username username, Password password)
      {
         OnPasswordHashBeginEvent(PasswordHashType.CredentialKey);
         await Task.Delay(1);

         uint hashKeySize = _cryptoProvider.GenericHash.KeySize;
         byte[] hashedUsername = _cryptoProvider.GenericHash.GenerateHash(hashKeySize, username.Value.ToLower());

         uint saltSize = _cryptoProvider.PasswordHash.SaltSize;
         byte[] salt = _cryptoProvider.GenericHash.GenerateHash(saltSize, password.Value, hashedUsername);

         Maybe<byte[]> hashResult = _cryptoProvider.PasswordHash.GenerateKey(password.Value, salt, _credentialKeySize, OpsLimit.Sensitive, MemLimit.Moderate)
            .ToMaybe();

         OnPasswordHashEndEvent(hashResult.IsSome);
         return hashResult;
      }

      private void OnPasswordHashBeginEvent(PasswordHashType hashType) =>
         _passwordHashBeginEventHandler?.Invoke(this, new PasswordHashBeginEventArgs(hashType));

      private void OnPasswordHashEndEvent(bool success) =>
         _passwordHashEndEventHandler?.Invoke(this, new PasswordHashEndEventArgs(success));

      public event EventHandler<PasswordHashBeginEventArgs> PasswordHashBeginEventHandler
      {
         add => _passwordHashBeginEventHandler = (EventHandler<PasswordHashBeginEventArgs>)Delegate.Combine(_passwordHashBeginEventHandler, value);
         remove => _passwordHashBeginEventHandler = (EventHandler<PasswordHashBeginEventArgs>)Delegate.Remove(_passwordHashBeginEventHandler, value);
      }

      public event EventHandler<PasswordHashEndEventArgs> PasswordHashEndEventHandler
      {
         add => _passwordHashEndEventHandler = (EventHandler<PasswordHashEndEventArgs>)Delegate.Combine(_passwordHashEndEventHandler, value);
         remove => _passwordHashEndEventHandler = (EventHandler<PasswordHashEndEventArgs>)Delegate.Remove(_passwordHashEndEventHandler, value);
      }
   }
}

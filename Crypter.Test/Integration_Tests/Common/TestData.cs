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

using Crypter.Common.Contracts.Features.UserAuthentication;
using Crypter.Common.Enums;
using Crypter.Crypto.Common.StreamEncryption;
using Crypter.Crypto.Providers.Default;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Crypter.Test.Integration_Tests.Common
{
   internal static class TestData
   {
      private static readonly byte[] _defaultPrivateKey = new byte[]
      {
         0xdf, 0x9f, 0x89, 0x8b, 0x66, 0xc9, 0xcc, 0x2c,
         0xb2, 0x9b, 0xa8, 0x51, 0xf7, 0xf7, 0x1c, 0xec,
         0x59, 0xad, 0x67, 0x09, 0xc7, 0x77, 0xab, 0x30,
         0x09, 0x3b, 0x52, 0xd6, 0xde, 0x2f, 0x5e, 0x28
      };

      private static readonly byte[] _alternatePrivateKey = new byte[]
      {
         0xe6, 0xdc, 0x7f, 0xaa, 0x13, 0x8d, 0xdc, 0xbe,
         0x23, 0xbe, 0xf7, 0xb0, 0x81, 0x10, 0x3b, 0xbd,
         0x6c, 0x63, 0x2b, 0xf5, 0xe2, 0x25, 0xfa, 0x8f,
         0xef, 0x3c, 0x66, 0x3f, 0x07, 0x49, 0x0a, 0xd2
      };

      private static readonly byte[] _defaultPublicKey = new byte[]
      {
         0x2f, 0x33, 0x2f, 0x8b, 0x19, 0x80, 0x12, 0x7f,
         0x7d, 0x0b, 0x5e, 0x00, 0x1a, 0x35, 0x28, 0x42,
         0x67, 0x96, 0x21, 0x3d, 0x04, 0x7e, 0x17, 0x6b,
         0xdc, 0x5f, 0xbc, 0xf2, 0x7e, 0x57, 0x61, 0x08
      };

      private static readonly byte[] _alternatePublicKey = new byte[]
      {
         0x89, 0xdf, 0xc0, 0xe1, 0x4a, 0x3f, 0x30, 0x08,
         0xb1, 0x4b, 0x29, 0x7b, 0xf7, 0x34, 0x82, 0xc6,
         0x97, 0x2c, 0x5f, 0x9b, 0xa3, 0xd3, 0x2a, 0xe2,
         0xb0, 0x32, 0xce, 0x2a, 0x9c, 0x34, 0xf8, 0x45
      };

      private static readonly byte[] _defaultKeyExchangeNonce = new byte[]
      {
         0xd5, 0x3a, 0xcf, 0xc3, 0x66, 0x3f, 0xfd, 0x80,
         0xbe, 0x9a, 0x5e, 0xc4, 0x1f, 0xa9, 0x56, 0xd7,
         0x89, 0xad, 0x3c, 0x67, 0x92, 0xa2, 0x7f, 0xa5,
         0x2c, 0x19, 0x9c, 0xd8, 0x0c, 0xed, 0xd5, 0x83
      };

      internal static byte[] DefaultPrivateKey
      {
         get => _defaultPrivateKey;
      }

      internal static byte[] AlternatePrivateKey
      {
         get => _alternatePrivateKey;
      }

      internal static byte[] DefaultPublicKey
      {
         get => _defaultPublicKey;
      }

      internal static byte[] AlternatePublicKey
      {
         get => _alternatePublicKey;
      }

      internal static byte[] DefaultKeyExchangeNonce
      {
         get => _defaultKeyExchangeNonce;
      }

      internal const string DefaultTransferFileName = "unit testing.txt";
      internal const string DefaultTransferFileContentType = "text/plain";
      internal const string DefaultTransferMessageSubject = "hello there";
      internal static byte[] DefaultTransferBytes => "unit testing is great"u8.ToArray();
      internal const int DefaultTransferLifetimeHours = 24;

      internal static (EncryptionStream encryptionStream, byte[] proof) GetDefaultEncryptionStream()
      {
         MemoryStream plaintextStream = new MemoryStream(DefaultTransferBytes);
         DefaultCryptoProvider cryptoProvider = new DefaultCryptoProvider();
         (byte[] encryptionKey, byte[] proof) = cryptoProvider.KeyExchange.GenerateEncryptionKey(cryptoProvider.StreamEncryptionFactory.KeySize, DefaultPrivateKey, AlternatePublicKey, DefaultKeyExchangeNonce);

         EncryptionStream encryptionStream = new EncryptionStream(plaintextStream, plaintextStream.Length, encryptionKey, cryptoProvider.StreamEncryptionFactory, 128, 64);
         return (encryptionStream, proof);
      }

      internal static RegistrationRequest GetRegistrationRequest(string username, string password, string emailAddress = null)
      {
         byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
         VersionedPassword versionedPassword = new VersionedPassword(passwordBytes, 1);
         return new RegistrationRequest(username, versionedPassword, emailAddress);
      }

      internal static LoginRequest GetLoginRequest(string username, string password, TokenType tokenType = TokenType.Session)
      {
         byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
         VersionedPassword versionedPassword = new VersionedPassword(passwordBytes, 1);
         return new LoginRequest(username, new List<VersionedPassword> { versionedPassword }, tokenType);
      }
   }
}

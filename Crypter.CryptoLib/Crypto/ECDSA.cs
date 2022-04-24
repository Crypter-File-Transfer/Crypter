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

using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Security;

namespace Crypter.CryptoLib.Crypto
{
   /// <summary>
   /// Elliptical curve, digital signature algorithm
   /// </summary>
   /// <remarks>
   /// https://github.com/bcgit/bc-csharp/blob/master/crypto/test/src/crypto/test/Ed25519Test.cs
   /// </remarks>
   public class ECDSA
   {
      private ISigner Signer;
      private ISigner Verifier;

      public static AsymmetricCipherKeyPair GenerateKeys()
      {
         SecureRandom random = new();
         Ed25519KeyPairGenerator kpg = new();
         kpg.Init(new Ed25519KeyGenerationParameters(random));
         return kpg.GenerateKeyPair();
      }

      public void InitializeSigner(AsymmetricKeyParameter privateKey)
      {
         Signer = new Ed25519Signer();
         Signer.Init(true, privateKey);
      }

      public void InitializeVerifier(AsymmetricKeyParameter publicKey)
      {
         Verifier = new Ed25519Signer();
         Verifier.Init(false, publicKey);
      }

      public void SignerDigestPart(byte[] data)
      {
         Signer.BlockUpdate(data, 0, data.Length);
      }

      public void VerifierDigestPart(byte[] data)
      {
         Verifier.BlockUpdate(data, 0, data.Length);
      }

      public byte[] GenerateSignature()
      {
         var signature = Signer.GenerateSignature();
         Signer.Reset();
         return signature;
      }

      public bool VerifySignature(byte[] signature)
      {
         var verified = Verifier.VerifySignature(signature);
         Verifier.Reset();
         return verified;
      }
   }
}

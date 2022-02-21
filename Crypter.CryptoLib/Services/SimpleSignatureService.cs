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

using Crypter.CryptoLib.Crypto;

namespace Crypter.CryptoLib.Services
{
   public interface ISimpleSignatureService
   {
      public byte[] Sign(string ed25519PrivateKeyPEM, byte[] data);
      public bool Verify(string ed25519PublicKeyPEM, byte[] data, byte[] signature);
   }

   public class SimpleSignatureService : ISimpleSignatureService
   {
      public byte[] Sign(string ed25519PrivateKeyPEM, byte[] data)
      {
         var privateKey = KeyConversion.ConvertEd25519PrivateKeyFromPEM(ed25519PrivateKeyPEM);

         var signer = new ECDSA();
         signer.InitializeSigner(privateKey);
         signer.SignerDigestChunk(data);
         return signer.GenerateSignature();
      }

      public bool Verify(string ed25519PublicKeyPEM, byte[] data, byte[] signature)
      {
         var publicKey = KeyConversion.ConvertEd25519PublicKeyFromPEM(ed25519PublicKeyPEM);

         var verifier = new ECDSA();
         verifier.InitializeVerifier(publicKey);
         verifier.VerifierDigestChunk(data);
         return verifier.VerifySignature(signature);
      }
   }
}

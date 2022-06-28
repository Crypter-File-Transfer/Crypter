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

using Crypter.Common.Monads;
using Crypter.Common.Primitives;
using Crypter.CryptoLib.Crypto;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Crypter.CryptoLib.Services
{
   public interface ISimpleSignatureService
   {
      byte[] Sign(PEMString ed25519PrivateKey, byte[] data);
      byte[] Sign(PEMString ed25519PrivateKey, string data);
      Task<byte[]> SignStreamAsync(PEMString ed25519PrivateKey, Stream stream, long streamLength, int partSize, Maybe<Func<double, Task>> progressFunc);
      bool Verify(PEMString ed25519PublicKey, byte[] data, byte[] signature);
   }

   public class SimpleSignatureService : ISimpleSignatureService
   {
      public byte[] Sign(PEMString ed25519PrivateKey, byte[] data)
      {
         var privateKey = KeyConversion.ConvertEd25519PrivateKeyFromPEM(ed25519PrivateKey);

         var signer = new ECDSA();
         signer.InitializeSigner(privateKey);
         signer.SignerDigestPart(data);
         return signer.GenerateSignature();
      }

      public byte[] Sign(PEMString ed25519PrivateKey, string data)
      {
         return Sign(ed25519PrivateKey, Encoding.UTF8.GetBytes(data));
      }

      public async Task<byte[]> SignStreamAsync(PEMString ed25519PrivateKey, Stream stream, long streamLength, int partSize, Maybe<Func<double, Task>> progressFunc)
      {
         await progressFunc.IfSomeAsync(async func => await func.Invoke(0.0));

         var privateKey = KeyConversion.ConvertEd25519PrivateKeyFromPEM(ed25519PrivateKey);

         var signer = new ECDSA();
         signer.InitializeSigner(privateKey);

         int bytesRead = 0;
         while (bytesRead + partSize < streamLength)
         {
            Console.WriteLine($"Signing bytes: {bytesRead}");
            byte[] readBuffer = new byte[partSize];
            bytesRead += await stream.ReadAsync(readBuffer.AsMemory(0, partSize));

            signer.SignerDigestPart(readBuffer);

            await progressFunc.IfSomeAsync(async func =>
            {
               double progress = (double)bytesRead / streamLength;
               await func.Invoke(progress);
            });
         }

         int finalPlaintextLength = Convert.ToInt32(streamLength) - bytesRead;
         byte[] finalReadBuffer = new byte[finalPlaintextLength];
         await stream.ReadAsync(finalReadBuffer.AsMemory(0, finalPlaintextLength));

         signer.SignerDigestPart(finalReadBuffer);
         byte[] signature = signer.GenerateSignature();
         await progressFunc.IfSomeAsync(async func => await func.Invoke(1.0));
         return signature;
      }

      public bool Verify(PEMString ed25519PublicKey, byte[] data, byte[] signature)
      {
         var publicKey = KeyConversion.ConvertEd25519PublicKeyFromPEM(ed25519PublicKey);

         var verifier = new ECDSA();
         verifier.InitializeVerifier(publicKey);
         verifier.VerifierDigestPart(data);
         return verifier.VerifySignature(signature);
      }
   }
}

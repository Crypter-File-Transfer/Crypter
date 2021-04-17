using Crypter.CryptoLib.BouncyCastle;
using Crypter.CryptoLib.Models;
using System;
using System.Text;
using System.Text.Json;

namespace Crypter.CryptoLib
{
   public static class Common
   {
      public static ClientEncryptResult DoAnonymousClientEncryption(byte[] plainBytes, RsaKeySize rsaKeySize)
      {
         // Encipher the plaintext
         var symmetricWrapper = new SymmetricWrapper();
         var symmetricKey = symmetricWrapper.GenerateSymmetricKey();
         var iv = symmetricWrapper.GenerateIV();
         var cipherText = symmetricWrapper.EncryptBytes(plainBytes, symmetricKey, iv);

         // Create a signature, which contains a hash of the plaintext and the plaintext public key
         byte[] messageHash = HashWrapper.GetSha256Digest(plainBytes);
         var encodedHash = Convert.ToBase64String(messageHash);
         var encodedSymmetricKey = Convert.ToBase64String(symmetricKey.ConvertToBytes());
         var signature = new AnonymousSignature(encodedHash, encodedSymmetricKey);
         var signatureJson = JsonSerializer.Serialize(signature);
         var signatureJsonBytes = Encoding.UTF8.GetBytes(signatureJson);

         // Encipher the signature using an RSA public key
         var asymmetricWrapper = new AsymmetricWrapper();
         var asymmetricKeyPair = asymmetricWrapper.GenerateAsymmetricKeyPair(rsaKeySize);
         var encryptedSignature = asymmetricWrapper.Encrypt(signatureJsonBytes, asymmetricKeyPair.Public);

         // Hash the symmetric key to create a server-side encryption key
         var hashedSymmetricKey = HashWrapper.GetSha256Digest(symmetricKey.ConvertToBytes());

         // Base64 encode everything that needs to be transmitted or displayed
         var encodedCipherText = Convert.ToBase64String(cipherText);
         var encodedSignature = Convert.ToBase64String(encryptedSignature);
         var encodedServerEncryptionKey = Convert.ToBase64String(hashedSymmetricKey);

         return new ClientEncryptResult(encodedCipherText, encodedSignature, encodedServerEncryptionKey, asymmetricKeyPair);
      }
   }
}
